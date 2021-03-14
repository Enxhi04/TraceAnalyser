using Analyser.VerificationValidation.Models.Cases;
using Analyser.VerificationValidation.Models.Cases.Logs;
using Analyser.VerificationValidation.Models.Cases.Procedures;
using Analyser.VerificationValidation.Models.Verifies;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Analyser.VerificationValidation
{
    public class ValidationWriter
    {
        private string path;
        private XElement parentNode;

        private XElement verifies;

        private XElement vvCases;
        private string vvCasesPath;

        private XNamespace ns;

        private XDocument logXmlWriter;

        XmlWriterSettings writerSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true
        };

        public ValidationWriter(string path, XNamespace ns)
        {
            this.ns = ns;
            this.path = path;

            logXmlWriter = XDocument.Load(path);
            parentNode = logXmlWriter.Descendants(ns + "VERIFICATION-VALIDATION").Single();
            verifies = parentNode.GetChildByLocalName("VERIFYS");
            vvCases = parentNode.GetChildByLocalName("VV-CASES");

            vvCasesPath = GetPath(vvCases);

        }

        public void Save()
        {
            using (XmlWriter xw = XmlWriter.Create(path, writerSettings))
            {
                logXmlWriter.Save(xw);
            }
        }

        public void WriteValidation(VerificationValidationLog log)
        {
            bool verifyExists = verifies.Descendants(ns + "VERIFIED-REQUIREMENT-REFS")
                .Any(x => x.Value == log.Constraint);

            if (!verifyExists)
            {
                CreateVerifyAndCase(log);
            }
            else
            {
                var vvCase = vvCases.Elements()
                    .Single(x => x.GetChildValueByLocalName("SHORT-NAME") == log.Constraint + "Case");
                var actualOutcomes = vvCase.Descendants(ns + "VV-ACTUAL-OUTCOMES").Single();

                var actualOutcomeNode = GetActualOutcome(log).Serialize(ns);
                actualOutcomes.Add(actualOutcomeNode);
            }
        }

        private void CreateVerifyAndCase(VerificationValidationLog log)
        {
            var comment = $"CONSTRAINT with SHORT-NAME {log.Constraint} AND NAME {log.Value} " +
                $"has stimulus signal {log.Stimulus} and response signal {log.Response}";
            var verify = new Verify()
            {
                Name = log.Constraint + "Verify",
                ShortName = log.Constraint + "Verify",
                VerifiedByCaseRefs = new List<VerifiedByCaseRef>() {
                    new VerifiedByCaseRef() {
                        Value = vvCasesPath + log.Constraint + "Case",
                        Type= "VV-CASE"
                    }
                },
                VerifiedRequirementRefs = log.Constraint,
                OwnedComments = comment
            };
            XElement verifyNode = verify.Serialize(ns);
            verifies.Add(verifyNode);

            var @case = new VvCase()
            {
                ShortName = log.Constraint + "Case",
                Name = log.Constraint + "Case",
                VvTargetRefs = new List<VvTargetRef>()
                {
                    new VvTargetRef() { Type = "VV-TARGET", Value = "/Extension/Extensions/VV/VVTarget_WithController" }
                },
                VvProcedures = new List<VvProcedure>()
                {
                     new VvProcedure()
                    {
                        VvIntendedOutcomes = new List<VvIntendedOutcome>()
                        {
                            new VvIntendedOutcome()
                            {
                                ShortName = log.Value.ToString()
                            }
                        }
                    }
                },
                VvLogs = new List<VvLog>()
                {
                    new VvLog()
                    {
                        VvActualOutcomes =new List<VvActualOutcome>()
                        {
                            GetActualOutcome(log)
                        }
                    }
                }
            };
            XElement caseNode = @case.Serialize(ns);
            vvCases.Add(caseNode);
        }

        private VvActualOutcome GetActualOutcome(VerificationValidationLog log)
        {
            var actualElapsed = (double.Parse(log.ResponseTimestamp) - double.Parse(log.StimulusTimestamp)).ToString();
            return new VvActualOutcome()
            {
                ShortName = actualElapsed,
                Name = actualElapsed,
                OwnedComments = $"Stimulus signal {log.Stimulus}:{log.StimulusTimestamp} " +
                   $"with response signal {log.Response}:{log.ResponseTimestamp} " +
                   $"failed for {log.ConstraintType} with NAME {log.Constraint}."
            };
        }


        private string GetPath(XElement element)
        {
            string path = "";
            var ancestors = element.Ancestors();
            foreach (var ancestor in ancestors)
            {
                var shortNameNode = ancestor.GetChildByLocalName("SHORT-NAME");
                if (shortNameNode != null)
                {
                    path = (shortNameNode.Value ?? "") + "/" + path;
                }

            }
            return path;
        }
    }
}
