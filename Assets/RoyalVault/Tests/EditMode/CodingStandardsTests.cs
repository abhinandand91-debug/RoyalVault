using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace RoyalVault.Tests
{
    // The C# rules in CLAUDE.md, checked on every source file we own
    public sealed class CodingStandardsTests
    {
        private static readonly string[] Roots = { "GameKit", "SlotKit", "RoyalVault" };

        private static readonly Regex ControlKeyword = new Regex(@"(?<![\w.#])(if|for|foreach|while)\s*\(");
        private static readonly Regex ElseKeyword = new Regex(@"(?<![\w.#])else\b");
        private static readonly Regex Accessors = new Regex(@"\{\s*((private|protected|internal)\s+)?(get|set|init)\b");

        [Test]
        public void ControlStatementsAlwaysUseBraces()
        {
            var violations = new List<string>();

            foreach (string path in SourceFiles())
            {
                string code = StripLiteralsAndComments(File.ReadAllText(path));
                violations.AddRange(UnbracedControls(path, code));
                violations.AddRange(UnbracedElses(path, code));
            }

            AssertNone(violations, "control statements without braces");
        }

        [Test]
        public void OneStatementPerLine()
        {
            var violations = new List<string>();

            foreach (string path in SourceFiles())
            {
                string[] lines = StripLiteralsAndComments(File.ReadAllText(path)).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (TopLevelSemicolons(lines[i]) > 1 && !Accessors.IsMatch(lines[i]))
                    {
                        violations.Add(Where(path, i + 1));
                    }
                }
            }

            AssertNone(violations, "lines with more than one statement");
        }

        [Test]
        public void OpeningBracesSitOnTheirOwnLine()
        {
            var violations = new List<string>();

            foreach (string path in SourceFiles())
            {
                string[] lines = StripLiteralsAndComments(File.ReadAllText(path)).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.EndsWith("{") && line != "{")
                    {
                        violations.Add(Where(path, i + 1));
                    }
                }
            }

            AssertNone(violations, "opening braces at the end of a line");
        }

        private static IEnumerable<string> SourceFiles()
        {
            foreach (string root in Roots)
            {
                string folder = Path.Combine(Application.dataPath, root);
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (string path in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
                {
                    yield return path;
                }
            }
        }

        // An if, for, foreach or while must open a block; a while may also close a do loop
        private static IEnumerable<string> UnbracedControls(string path, string code)
        {
            foreach (Match match in ControlKeyword.Matches(code))
            {
                int close = ClosingParen(code, match.Index + match.Length - 1);
                char next = NextVisible(code, close + 1);
                bool doLoopEnd = match.Groups[1].Value == "while" && next == ';';

                if (next != '{' && !doLoopEnd)
                {
                    yield return Where(path, LineOf(code, match.Index));
                }
            }
        }

        // An else must open a block or continue with another if
        private static IEnumerable<string> UnbracedElses(string path, string code)
        {
            foreach (Match match in ElseKeyword.Matches(code))
            {
                int start = match.Index + match.Length;
                char next = NextVisible(code, start);
                string rest = code.Substring(start).TrimStart();

                if (next != '{' && !rest.StartsWith("if"))
                {
                    yield return Where(path, LineOf(code, match.Index));
                }
            }
        }

        private static int TopLevelSemicolons(string line)
        {
            int depth = 0;
            int count = 0;

            foreach (char c in line)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if (c == ';' && depth == 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ClosingParen(string code, int open)
        {
            int depth = 0;

            for (int i = open; i < code.Length; i++)
            {
                if (code[i] == '(')
                {
                    depth++;
                }
                else if (code[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return code.Length - 1;
        }

        private static char NextVisible(string code, int from)
        {
            for (int i = from; i < code.Length; i++)
            {
                if (!char.IsWhiteSpace(code[i]))
                {
                    return code[i];
                }
            }

            return '\0';
        }

        private static int LineOf(string code, int index)
        {
            return code.Take(index).Count(c => c == '\n') + 1;
        }

        private static string Where(string path, int line)
        {
            string relative = path.Substring(Application.dataPath.Length + 1).Replace('\\', '/');
            return relative + ":" + line;
        }

        private static void AssertNone(List<string> violations, string what)
        {
            string list = string.Join("\n", violations.Take(30));
            Assert.IsEmpty(violations, violations.Count + " " + what + " (see CLAUDE.md):\n" + list);
        }

        // Blanks out strings, chars and comments, keeping line breaks so line numbers stay right
        private static string StripLiteralsAndComments(string code)
        {
            var result = new StringBuilder(code.Length);
            int i = 0;

            while (i < code.Length)
            {
                if (StartsWith(code, i, "//"))
                {
                    i = SkipTo(code, i, "\n", result, false);
                }
                else if (StartsWith(code, i, "/*"))
                {
                    i = SkipTo(code, i + 2, "*/", result, true);
                }
                else if (code[i] == '"' || code[i] == '\'')
                {
                    i = SkipLiteral(code, i, result);
                }
                else
                {
                    result.Append(code[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        private static bool StartsWith(string code, int index, string text)
        {
            return string.CompareOrdinal(code, index, text, 0, text.Length) == 0;
        }

        private static int SkipTo(string code, int index, string end, StringBuilder result, bool consumeEnd)
        {
            int stop = code.IndexOf(end, index, System.StringComparison.Ordinal);
            if (stop < 0)
            {
                stop = code.Length;
            }

            foreach (char c in code.Substring(index, stop - index).Where(c => c == '\n'))
            {
                result.Append(c);
            }

            return consumeEnd ? Mathf.Min(code.Length, stop + end.Length) : stop;
        }

        private static int SkipLiteral(string code, int index, StringBuilder result)
        {
            char quote = code[index];
            bool verbatim = index > 0 && code[index - 1] == '@';
            int i = index + 1;

            while (i < code.Length && code[i] != quote)
            {
                if (code[i] == '\\' && !verbatim)
                {
                    i++;
                }

                i++;
            }

            result.Append(quote).Append(quote);
            return i + 1;
        }
    }
}
