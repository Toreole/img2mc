using System.Text;
using System.Text.RegularExpressions;

namespace TexMetadataGenerator;

public static partial class BlockNameHelper
{
    public static string GetNaiveBlockNameFromFile(string fileName)
    {
        StringBuilder builder = new();
        bool start = true;
        foreach (var c in fileName)
        {
            if (c == '_')
            {
                builder.Append(' ');
                start = true;
                continue;
            }
            if (c == '.')
            {
                break;
            }
            builder.Append(start ? char.ToUpper(c) : c);
            start = false;
        }
        return builder.ToString();
    }

    private static readonly Regex unwantedSub = UnwantedPartsOfNameRegex();

    public static string RemoveFaceName(string blockName)
    {
        var match = unwantedSub.Match(blockName);
        return match.Success ? blockName[..match.Index] : blockName;
    }

    [GeneratedRegex(" (Side[0-9]?|Top|Front|Bottom|End|On)$")]
    private static partial Regex UnwantedPartsOfNameRegex();

    public static string GetBlockName(string fileName)
    {
        return RemoveFaceName(GetNaiveBlockNameFromFile(fileName));
    }
}
