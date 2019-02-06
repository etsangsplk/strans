﻿using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using CommandLine;
using CommandLine.Text;

namespace CommandLine.Text
{
    // HelpText="Performs string manipulation tasks by learning from the provided example(s), instead of having to program them out explicitly.
    class Options
    {
        [Option('b', "before", SetName = "SingleExample", Required = true, HelpText = "An example of an input line before transformation")]
        public String before { get; set; }
        [Option('a', "after", SetName = "SingleExample", Required = true, HelpText = "An example of an output line after transformation")]
        public String after { get; set; }

        [Option('f', "example-file", SetName = "MultiExample", Required = true, HelpText = "A file containing one or multiple transformation examples. The before and after transfer string are separated by => on the same line. One line per example.")]
        public String exampleFile { get; set; }

        [Usage(ApplicationAlias = "strans")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Standard usage where strans infers a string transformation rule from one example, extracting the file extension from the input on STDIN (for example, from ls)", new Options { before = "file.bin", after = "bin" });
                yield return new Example("Usage with multiple examples to infer string transformation rules stored in file examples. Syntax in example is one transformation example per line as before => after", new Options { exampleFile = "examples" });
            }
        }


    }
}

namespace Microsoft.ProgramSynthesis.Transformation.Text
{

    class MyProgram
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    options => run(options),
                    _ => 1);
        }

        static int run(Options options)
        {
            Session session = new Session();

            HashSet<Example> examples;
            if (options.exampleFile != null)
            {
                examples = buildExamples(options.exampleFile);
            }
            else
            {
                examples = buildExamples(options);
            }

            if (examples.Count < 1)
            {
                Console.Error.WriteLine(@"Error: You need to provide at least one valid transformation example of form 'before => after'.");
                return -1;
            }

            session.Constraints.Add(examples);
            Program program = session.Learn();

            if (program == null)
            {
                Console.Error.WriteLine(@"Error: Given your transformation examples, no program could be learned.");
                return -1;
            }

            processInputPipe(program);

            return 0;
        }

        static HashSet<Example> buildExamples(Options options)
        {
            HashSet<Example> examples = new HashSet<Example>();
            var before = options.before.Trim();
            var after = options.after.Trim();

            examples.Add(new Example(new InputRow(before), after));

            return examples;
        }

        static HashSet<Example> buildExamples(string file)
        {
            HashSet<Example> examples = new HashSet<Example>();
            string[] lines = System.IO.File.ReadAllLines(file);

            foreach (string line in lines)
            {
                if (line.Trim().Equals(System.String.Empty))
                {
                    continue;
                }

                var example = line.Split(@"=>");
                if (example.Length != 2)
                {
                    Console.Error.WriteLine(@"Wrongly formatted line in examples: " + line);
                }
                else
                {
                    examples.Add(new Example(new InputRow(example[0].Trim()), example[1].Trim()));
                }
            }
            return examples;
        }


        static void processInputPipe(Program program)
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                Console.WriteLine(program.Run(new InputRow(line)));
            }
        }
    }
}