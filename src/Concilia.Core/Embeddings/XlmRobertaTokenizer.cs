using Microsoft.ML.Tokenizers;

namespace Concilia.Core.Embeddings;

// The tokenizer of paraphrase-multilingual-MiniLM-L12-v2, the same as
// XLM-RoBERTa's: a SentencePiece (unigram) model, sentencepiece.bpe.model,
// read with Microsoft.ML.Tokenizers.
//
// The model was trained with the ids of fairseq, not with the ones in the
// SentencePiece file, so they are translated here as Hugging Face does
// (XLMRobertaTokenizer in transformers):
//   SentencePiece file:  <unk> = 0, <s> = 1, </s> = 2, the pieces from 3 on
//   model (fairseq):     <s> = 0, <pad> = 1, </s> = 2, <unk> = 3, every
//                        piece one place further (its file id + 1)
// The tests check the result against the Hugging Face tokenizer.
public sealed class XlmRobertaTokenizer
{
    public const long BeginId = 0;    // <s>, at the start of every text
    public const long PaddingId = 1;  // <pad>, to fill the shorter texts of a batch
    public const long EndId = 2;      // </s>, at the end of every text
    public const long UnknownId = 3;  // <unk>

    // A piece's id in the model is its id in the SentencePiece file plus one.
    private const int FairseqOffset = 1;

    private readonly SentencePieceTokenizer _sentencePiece;

    public XlmRobertaTokenizer(Stream sentencePieceModel)
    {
        // <s> and </s> are added below with the model's ids, not by the library.
        _sentencePiece = SentencePieceTokenizer.Create(sentencePieceModel, addBeginningOfSentence: false, addEndOfSentence: false);
    }

    public static XlmRobertaTokenizer FromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return new XlmRobertaTokenizer(stream);
    }

    // The ids of a text as the model expects them: <s>, the pieces and </s>,
    // with at most maxTokens in all. A longer text loses its end, as in
    // sentence-transformers (max_seq_length = 128 for this model).
    public long[] Encode(string text, int maxTokens)
    {
        if (maxTokens < 2)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "Hacen falta al menos dos tokens: <s> y </s>.");

        var pieces = _sentencePiece.EncodeToIds(text, addBeginningOfSentence: false, addEndOfSentence: false);
        var kept = Math.Min(pieces.Count, maxTokens - 2);

        var ids = new long[kept + 2];
        ids[0] = BeginId;
        for (var i = 0; i < kept; i++)
            ids[i + 1] = pieces[i] == _sentencePiece.UnknownId ? UnknownId : pieces[i] + FairseqOffset;
        ids[^1] = EndId;
        return ids;
    }
}
