﻿// Optimized by Richard Deeming
// Original code by Vasian Cepa
// https://madebits.github.io/#r/msnet-numeric-sort.md

#if LINUX
namespace Moder.Core.Infrastructure.FileSort;

public sealed class LinuxFileSortComparer : IFileSortComparer
{
    public int Compare(string? s1, string? s2)
    {
        return Compare(s1, s2, false);
    }

    // zeroesFirst 参数为 true 时, 字符串前带有前导零的数字按默认顺序排列, 与 Windows 资源管理器中的顺序相同
    // 001
    // 01
    // 1
    // 002
    // 02
    // 2
    // 为 false 时:
    // 001
    // 002
    // 01
    // 02
    // 1
    // 2
    private static int Compare(string? s1, string? s2, bool zeroesFirst)
    {
        if (string.IsNullOrEmpty(s1))
        {
            if (string.IsNullOrEmpty(s2))
            {
                return 0;
            }

            return -1;
        }

        if (string.IsNullOrEmpty(s2))
        {
            return 1;
        }

        var s1Length = s1.Length;
        var s2Length = s2.Length;

        var sp1 = char.IsLetterOrDigit(s1[0]);
        var sp2 = char.IsLetterOrDigit(s2[0]);

        if (sp1 && !sp2)
        {
            return 1;
        }

        if (!sp1 && sp2)
        {
            return -1;
        }

        char c1,
            c2;
        int i1 = 0,
            i2 = 0;
        int r;
        bool letter1,
            letter2;

        while (true)
        {
            c1 = s1[i1];
            c2 = s2[i2];

            sp1 = char.IsDigit(c1);
            sp2 = char.IsDigit(c2);

            if (!sp1 && !sp2)
            {
                if (c1 != c2)
                {
                    letter1 = char.IsLetter(c1);
                    letter2 = char.IsLetter(c2);

                    if (letter1 && letter2)
                    {
                        c1 = char.ToUpper(c1);
                        c2 = char.ToUpper(c2);

                        r = c1 - c2;
                        if (0 != r)
                        {
                            return r;
                        }
                    }
                    else if (!letter1 && !letter2)
                    {
                        r = c1 - c2;
                        if (0 != r)
                        {
                            return r;
                        }
                    }
                    else if (letter1)
                    {
                        return 1;
                    }
                    else if (letter2)
                    {
                        return -1;
                    }
                }
            }
            else if (sp1 && sp2)
            {
                r = CompareNumbers(s1, s1Length, ref i1, s2, s2Length, ref i2, zeroesFirst);
                if (0 != r)
                {
                    return r;
                }
            }
            else if (sp1)
            {
                return -1;
            }
            else if (sp2)
            {
                return 1;
            }

            i1++;
            i2++;

            if (i1 >= s1Length)
            {
                if (i2 >= s2Length)
                {
                    return 0;
                }

                return -1;
            }
            else if (i2 >= s2Length)
            {
                return 1;
            }
        }
    }

    private static int CompareNumbers(
        string s1,
        int s1Length,
        ref int i1,
        string s2,
        int s2Length,
        ref int i2,
        bool zeroesFirst
    )
    {
        int nzStart1 = i1,
            nzStart2 = i2;
        int end1 = i1,
            end2 = i2;

        ScanNumber(s1, s1Length, i1, ref nzStart1, ref end1);
        ScanNumber(s2, s2Length, i2, ref nzStart2, ref end2);

        var start1 = i1;
        i1 = end1 - 1;
        var start2 = i2;
        i2 = end2 - 1;

        if (zeroesFirst)
        {
            var zl1 = nzStart1 - start1;
            var zl2 = nzStart2 - start2;
            if (zl1 > zl2)
            {
                return -1;
            }

            if (zl1 < zl2)
            {
                return 1;
            }
        }

        var length1 = end2 - nzStart2;
        var length2 = end1 - nzStart1;

        if (length1 == length2)
        {
            int r;
            for (int j1 = nzStart1, j2 = nzStart2; j1 <= i1; j1++, j2++)
            {
                r = s1[j1] - s2[j2];
                if (0 != r)
                {
                    return r;
                }
            }

            length1 = end1 - start1;
            length2 = end2 - start2;

            if (length1 == length2)
            {
                return 0;
            }
        }

        if (length1 > length2)
        {
            return -1;
        }

        return 1;
    }

    private static void ScanNumber(string s, int length, int start, ref int nzStart, ref int end)
    {
        nzStart = start;
        end = start;

        var countZeros = true;
        var c = s[end];

        while (true)
        {
            if (countZeros)
            {
                if ('0' == c)
                {
                    nzStart++;
                }
                else
                {
                    countZeros = false;
                }
            }

            end++;
            if (end >= length)
            {
                break;
            }

            c = s[end];
            if (!char.IsDigit(c))
            {
                break;
            }
        }
    }
}
#endif
