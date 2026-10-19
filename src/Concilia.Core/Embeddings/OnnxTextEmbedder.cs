using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Concilia.Core.Embeddings;

// The real embedder (decision of the author, 16-10-2026): the model
// paraphrase-multilingual-MiniLM-L12-v2 (Apache 2.0, Spanish and English
// among 50+ languages), exported to ONNX by sentence-transformers and run
// with ONNX Runtime inside the API. Nothing leaves the machine.
//
// For each text: tokenizer → model (one 384-value vector per token) → mean of
// the token vectors (VectorMath.MeanPooling). It reproduces what
// sentence-transformers does with this model; the tests compare both.
public sealed class OnnxTextEmbedder : ITextEmbedder, IDisposable
{
    // The two files the folder of the model must hold.
    public const string ModelFileName = "model.onnx";
    public const string TokenizerFileName = "sentencepiece.bpe.model";

    // Of this model (sentence_bert_config.json and 1_Pooling/config.json).
    public const int MaxTokens = 128;
    private const int VectorSize = 384;

    // Texts per call to the model. Larger batches go a little faster but
    // use more memory; 32 is the default of sentence-transformers.
    private const int BatchSize = 32;

    private readonly XlmRobertaTokenizer _tokenizer;

    // The model (about 470 MB) is loaded on first use, so the API starts fast
    // and phases that never compare texts do not pay for it.
    private readonly Lazy<InferenceSession> _session;

    public OnnxTextEmbedder(string modelFolder)
    {
        var model = Path.Combine(modelFolder, ModelFileName);
        var tokenizer = Path.Combine(modelFolder, TokenizerFileName);
        foreach (var file in new[] { model, tokenizer })
            if (!File.Exists(file))
                throw new FileNotFoundException(
                    $"Falta el fichero del modelo de embeddings: {file}. " +
                    "En local, descárgalo con scripts/descargar-modelo-embeddings.ps1; la imagen de Docker lo trae.", file);

        _tokenizer = XlmRobertaTokenizer.FromFile(tokenizer);
        _session = new Lazy<InferenceSession>(() => new InferenceSession(model));
    }

    public int Dimensions => VectorSize;

    public IReadOnlyList<float[]> Embed(IReadOnlyList<string> texts)
    {
        var vectors = new List<float[]>(texts.Count);
        for (var start = 0; start < texts.Count; start += BatchSize)
        {
            var batch = texts.Skip(start).Take(BatchSize).Select(text => _tokenizer.Encode(text, MaxTokens)).ToList();
            vectors.AddRange(EmbedBatch(batch));
        }
        return vectors;
    }

    // The model takes a rectangle [texts × tokens]: the shorter texts are
    // filled with <pad> up to the longest one, with mask 0 so neither the
    // model nor the mean pays attention to the padding.
    private List<float[]> EmbedBatch(List<long[]> batch)
    {
        var rows = batch.Count;
        var columns = batch.Max(ids => ids.Length);
        var inputIds = new DenseTensor<long>([rows, columns]);
        var attentionMask = new DenseTensor<long>([rows, columns]);
        var tokenTypeIds = new DenseTensor<long>([rows, columns]); // all 0: one sentence per text
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var isToken = column < batch[row].Length;
                inputIds[row, column] = isToken ? batch[row][column] : XlmRobertaTokenizer.PaddingId;
                attentionMask[row, column] = isToken ? 1 : 0;
            }

        var session = _session.Value;
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
        };
        if (session.InputMetadata.ContainsKey("token_type_ids"))
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds));

        // The first output is last_hidden_state: [texts × tokens × 384].
        using var outputs = session.Run(inputs);
        var hidden = outputs[0].AsTensor<float>().ToArray();

        var vectors = new List<float[]>(rows);
        var mask = attentionMask.ToArray();
        for (var row = 0; row < rows; row++)
        {
            var tokens = hidden.AsSpan(row * columns * VectorSize, columns * VectorSize);
            vectors.Add(VectorMath.MeanPooling(tokens, mask.AsSpan(row * columns, columns), VectorSize));
        }
        return vectors;
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value.Dispose();
    }
}
