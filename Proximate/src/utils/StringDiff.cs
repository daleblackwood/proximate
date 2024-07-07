using DiffMatchPatch;

namespace Proximate;

public static class StringDiff
{
    public static string Apply(string s, string patchStr)
    {
        var dmp = new diff_match_patch();
        var patches = dmp.patch_fromText(patchStr);
        var result = dmp.patch_apply(patches, s);
        return result[0] as string ?? s;
    }

    public static string Calculate(string prev, string value)
    {
        var dmp = new diff_match_patch();
        var diffs = dmp.diff_main(prev, value);
        dmp.diff_cleanupEfficiency(diffs);
        var patches = dmp.patch_make(diffs);
        var patchStr = dmp.patch_toText(patches);
        return patchStr;
    }
}
