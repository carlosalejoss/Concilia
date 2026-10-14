using Concilia.Core.Flow;

namespace Concilia.Tests.Flow;

// One test per rule of the v0 flow (docs/plan-de-construccion.md, "Estado de
// cada fase" and "Desactualización").
public class ProjectFlowTests
{
    private static StepStatus Step(FlowFacts facts, FlowStep step) => ProjectFlow.Compute(facts).Single(s => s.Step == step);

    private static readonly FlowFacts ReadyForCatalog = new() { TextFiles = 1, FinishedQuestionRounds = 1 };

    [Fact]
    public void The_page_has_the_five_steps_in_order()
    {
        Assert.Equal([FlowStep.Documents, FlowStep.Questions, FlowStep.Catalog, FlowStep.Backlog, FlowStep.Mockup],
            ProjectFlow.Compute(new FlowFacts()).Select(s => s.Step));
    }

    [Fact]
    public void Documents_can_always_be_uploaded()
    {
        Assert.True(Step(new FlowFacts(), FlowStep.Documents).CanLaunch);
        Assert.Equal("2 archivos con texto", Step(new FlowFacts { TextFiles = 2 }, FlowStep.Documents).Detail);
    }

    // Reading by parts (09-10-2026): the size of the documentation, a warning
    // when it does not fit in the window at once, and another over the limit.
    [Fact]
    public void The_documents_step_warns_about_big_documentation()
    {
        var small = Step(new FlowFacts { TextFiles = 1, DocumentationTokens = 2_000, MaxDocumentationTokens = 300_000 }, FlowStep.Documents);
        var big = Step(new FlowFacts { TextFiles = 3, DocumentationTokens = 95_000, MaxDocumentationTokens = 300_000 }, FlowStep.Documents);
        var tooBig = Step(new FlowFacts { TextFiles = 9, DocumentationTokens = 400_000, MaxDocumentationTokens = 300_000 }, FlowStep.Documents);

        Assert.StartsWith("1 archivo con texto · unos ", small.Detail);
        Assert.Null(small.Warning);
        Assert.StartsWith("La documentación no cabe de una vez", big.Warning);
        Assert.StartsWith("La documentación supera el máximo", tooBig.Warning);
    }

    [Fact]
    public void Questions_need_a_file_with_text()
    {
        var step = Step(new FlowFacts(), FlowStep.Questions);

        Assert.Equal(StepState.Blocked, step.State);
        Assert.False(step.CanLaunch);
        Assert.Equal("Sube al menos un archivo con texto.", step.Reason);
        Assert.True(Step(new FlowFacts { TextFiles = 1 }, FlowStep.Questions).CanLaunch);
    }

    [Fact]
    public void Rounds_can_be_launched_at_any_time_but_not_two_at_once()
    {
        Assert.True(Step(new FlowFacts { TextFiles = 1, FinishedQuestionRounds = 3, PendingQuestions = 5 }, FlowStep.Questions).CanLaunch);

        var running = Step(new FlowFacts { TextFiles = 1, Running = new HashSet<FlowStep> { FlowStep.Questions } }, FlowStep.Questions);
        Assert.Equal(StepState.InProgress, running.State);
        Assert.False(running.CanLaunch);
    }

    [Fact]
    public void The_catalog_needs_a_finished_round()
    {
        var step = Step(new FlowFacts { TextFiles = 1 }, FlowStep.Catalog);

        Assert.Equal(StepState.Blocked, step.State);
        Assert.Equal("Termina al menos una ronda de preguntas.", step.Reason);
    }

    [Fact]
    public void The_catalog_needs_no_pending_question()
    {
        var step = Step(ReadyForCatalog with { PendingQuestions = 2 }, FlowStep.Catalog);

        Assert.False(step.CanLaunch);
        Assert.Equal("Contesta o descarta 2 preguntas pendientes.", step.Reason);
    }

    [Fact]
    public void A_finished_round_without_questions_is_enough_for_the_catalog()
    {
        var step = Step(ReadyForCatalog, FlowStep.Catalog);

        Assert.Equal(StepState.Available, step.State);
        Assert.True(step.CanLaunch);
    }

    [Fact]
    public void A_generated_catalog_is_a_draft_until_it_is_validated()
    {
        Assert.Equal(StepState.Draft, Step(ReadyForCatalog with { Catalog = new(false, false) }, FlowStep.Catalog).State);
        Assert.Equal(StepState.Validated, Step(ReadyForCatalog with { Catalog = new(true, false) }, FlowStep.Catalog).State);
    }

    [Fact]
    public void A_catalog_keeps_its_state_when_a_new_round_leaves_questions_pending()
    {
        var step = Step(ReadyForCatalog with { PendingQuestions = 1, Catalog = new(true, true) }, FlowStep.Catalog);

        Assert.Equal(StepState.Validated, step.State);
        Assert.True(step.Outdated);
        Assert.False(step.CanLaunch); // regenerating it needs the pending question answered
    }

    [Fact]
    public void The_backlog_needs_the_validated_catalog()
    {
        Assert.Equal("Valida el catálogo.", Step(ReadyForCatalog, FlowStep.Backlog).Reason);
        Assert.Equal("Valida el catálogo.", Step(ReadyForCatalog with { Catalog = new(false, false) }, FlowStep.Backlog).Reason);
        Assert.True(Step(ReadyForCatalog with { Catalog = new(true, false) }, FlowStep.Backlog).CanLaunch);
    }

    [Fact]
    public void The_mockup_needs_the_validated_backlog()
    {
        var withCatalog = ReadyForCatalog with { Catalog = new(true, false) };

        Assert.Equal("Valida el backlog.", Step(withCatalog with { Backlog = new(false, false) }, FlowStep.Mockup).Reason);
        Assert.True(Step(withCatalog with { Backlog = new(true, false) }, FlowStep.Mockup).CanLaunch);
    }

    [Fact]
    public void A_job_in_progress_blocks_launching_its_step_again()
    {
        var step = Step(ReadyForCatalog with { Running = new HashSet<FlowStep> { FlowStep.Catalog } }, FlowStep.Catalog);

        Assert.Equal(StepState.InProgress, step.State);
        Assert.False(step.CanLaunch);
    }

    [Theory]
    [InlineData(FlowChange.FileAdded)]
    [InlineData(FlowChange.AnswerChanged)]
    [InlineData(FlowChange.QuestionDiscarded)]
    public void A_new_file_or_answer_outdates_the_catalog_the_backlog_and_the_mockup(FlowChange change)
    {
        Assert.Equal([FlowStep.Catalog, FlowStep.Backlog, FlowStep.Mockup], ProjectFlow.OutdatedBy(change));
    }

    [Theory]
    [InlineData(FlowChange.CatalogGenerated)]
    [InlineData(FlowChange.CatalogReopened)]
    [InlineData(FlowChange.CatalogValidated)]
    public void A_change_in_the_catalog_outdates_the_backlog_and_the_mockup(FlowChange change)
    {
        Assert.Equal([FlowStep.Backlog, FlowStep.Mockup], ProjectFlow.OutdatedBy(change));
    }

    [Theory]
    [InlineData(FlowChange.BacklogGenerated)]
    [InlineData(FlowChange.BacklogReopened)]
    [InlineData(FlowChange.BacklogValidated)]
    [InlineData(FlowChange.StyleAdded)]
    public void A_change_in_the_backlog_or_the_style_outdates_the_mockup(FlowChange change)
    {
        Assert.Equal([FlowStep.Mockup], ProjectFlow.OutdatedBy(change));
    }

    [Fact]
    public void An_outdated_deliverable_can_be_generated_again()
    {
        var step = Step(ReadyForCatalog with { Catalog = new(true, false), Backlog = new(true, true) }, FlowStep.Backlog);

        Assert.True(step.Outdated);
        Assert.True(step.CanLaunch);
    }
}
