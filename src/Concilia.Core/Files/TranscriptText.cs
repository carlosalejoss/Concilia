using System.Net;
using System.Text.RegularExpressions;

namespace Concilia.Core.Files;

// The transcript of a meeting (.vtt, from Teams or Zoom, or .srt subtitles)
// without its timings: what each person says, as "Name: text". Consecutive
// cues of the same speaker are joined into one turn, since a transcript cuts
// the speech every few seconds, often in the middle of a sentence.
//
//   00:00:01.000 --> 00:00:04.000
//   <v Ana Gómez>Necesitamos reservar salas</v>
internal static class TranscriptText
{
    private static readonly Regex Blank = new(@"\n\s*\n");
    // The speaker of a WebVTT cue: <v Ana Gómez> or <v.class Ana Gómez>.
    private static readonly Regex Voice = new(@"^<v(?:\.[^\s>]*)?\s+([^>]*)>");
    private static readonly Regex Tag = new(@"<[^>]*>");

    public static string Convert(string text)
    {
        var turns = new List<(string? Speaker, string Text)>();
        // A cue is a block of lines: an optional id, the timing line ("-->") and
        // the text. Blocks without timing are the header (WEBVTT), notes or styles.
        foreach (var block in Blank.Split(text.Trim()))
        {
            var lines = block.Split('\n');
            var timing = Array.FindIndex(lines, line => line.Contains("-->"));
            if (timing < 0)
            {
                continue;
            }

            var cue = string.Join(" ", lines[(timing + 1)..]).Trim();
            var voice = Voice.Match(cue);
            var speaker = voice.Success ? MarkdownText.OneLine(voice.Groups[1].Value) : null;
            var said = MarkdownText.OneLine(WebUtility.HtmlDecode(Tag.Replace(cue, "")));
            if (said.Length == 0)
            {
                continue;
            }

            if (turns.Count > 0 && turns[^1].Speaker == speaker)
                turns[^1] = (speaker, $"{turns[^1].Text} {said}");
            else
                turns.Add((speaker, said));
        }
        return MarkdownText.Blocks(turns.Select(t => t.Speaker is null ? t.Text : $"{t.Speaker}: {t.Text}"));
    }
}
