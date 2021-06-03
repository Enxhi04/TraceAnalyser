using Analyser.Constraints;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    public class ConstraintsProvider
    {
        private XDocument _xml;
        private XNamespace _ns;
        private IEnumerable<XElement> allConstraintXmlElements;

        public ConstraintsProvider(string path, string ns)
        {
            _xml = XDocument.Load(path);
            _ns = ns;
            allConstraintXmlElements = _xml.Descendants(_ns + "CONSTRAINTS")
                .Elements();
        }

        public List<AgeConstraint> GetAllAgeConstraints()
        {
            var eventChains = _xml.Descendants(_ns + "EVENT-CHAIN").Elements();
            var eventFunctions = _xml.Descendants(_ns + "EVENT-FUNCTION-FLOW-PORT").Elements();
            var ageConstraintsElements = allConstraintXmlElements.Where(x => x.Name.LocalName == "AGE-CONSTRAINT");

            var ageConstraints = new List<AgeConstraint>();
            foreach (var element in ageConstraintsElements)
            {
                var constraintShortName = element.GetChildValueByLocalName("SHORT-NAME");

                var constraintValue = double.Parse(element.GetChildValueByLocalName("NAME"), CultureInfo.InvariantCulture);
                var scopeRef = FindResourceName(element.GetChildValueByLocalName("SCOPE-REF"));

                List<StimulusResponse> stimulusResponses = new List<StimulusResponse>();
                FindStimulusResponses(eventChains, eventFunctions, scopeRef, ref stimulusResponses);

                var constaint = new AgeConstraint()
                {
                    ShortName = constraintShortName,
                    Value = constraintValue,
                    StimulusResponses = stimulusResponses
                };
                ageConstraints.Add(constaint);
            }

            return ageConstraints;
        }

        public List<ReactionConstraint> GetAllReactionConstraints()
        {
            var eventChains = _xml.Descendants(_ns + "EVENT-CHAIN").Elements();
            var eventFunctions = _xml.Descendants(_ns + "EVENT-FUNCTION-FLOW-PORT").Elements();
            var reactionConstraintElements = allConstraintXmlElements.Where(x => x.Name.LocalName == "REACTION-CONSTRAINT");

            var reactionConstraints = new List<ReactionConstraint>();
            foreach (var element in reactionConstraintElements)
            {
                var constraintShortName = element.GetChildValueByLocalName("SHORT-NAME");

                var constraintValue = double.Parse(element.GetChildValueByLocalName("NAME"), CultureInfo.InvariantCulture);
                var scopeRef = FindResourceName(element.GetChildValueByLocalName("SCOPE-REF"));

                var constaint = new ReactionConstraint()
                {
                    ShortName = constraintShortName,
                    Value = constraintValue
                };


                var resourceParent = eventChains.Single(x => x.Name.LocalName == "SHORT-NAME" && x.Value == scopeRef).Parent;
                var stimulusPortName = FindPortName(resourceParent, eventFunctions, "STIMULUS-REF");
                var responsePortName = FindPortName(resourceParent, eventFunctions, "RESPONSE-REF");

                constaint.InitialStimulusResponse = new StimulusResponse()
                {
                    Stimulus = stimulusPortName,
                    Response = responsePortName
                };

                var segRefs = resourceParent.Elements()
                               .First(c => c.Name.LocalName == "SEGMENT-REFS");

                if (segRefs.HasElements)
                {
                    foreach (var segmentRef in segRefs.GetChildrenValueByLocalName("SEGMENT-REF"))
                    {
                        var seg = FindResourceName(segmentRef);

                        List<StimulusResponse> stimulusResponses = new List<StimulusResponse>();

                        FindStimulusResponses(eventChains, eventFunctions, seg, ref stimulusResponses);

                        constaint.StimulusResponses.Add(stimulusResponses);
                    }
                }

                reactionConstraints.Add(constaint);
            }

            return reactionConstraints;
        }

        public List<DelayConstraint> GetAllDelayConstraints()
        {
            var delayConstraints = new List<DelayConstraint>();

            var eventFunctions = _xml.Descendants(_ns + "EVENT-FUNCTION-FLOW-PORT").Elements();

            foreach (var element in allConstraintXmlElements.Where(x => x.Name.LocalName == "DELAY-CONSTRAINT"))

            {
                var constraintShortName = element.GetChildValueByLocalName("SHORT-NAME");

                var constraintValue = double.Parse(element.GetChildValueByLocalName("NAME"), CultureInfo.InvariantCulture);

                var responsePortName = FindPortName(element, eventFunctions, "TARGET-REF");
                var stimulusPortName = FindPortName(element, eventFunctions, "SOURCE-REF");

                var stimulusResponse = new StimulusResponse() { Stimulus = stimulusPortName, Response = responsePortName };

                var delayConstraint = new DelayConstraint()
                {
                    ShortName = constraintShortName,
                    Value = constraintValue,
                    StimulusResponse = stimulusResponse
                };
                delayConstraints.Add(delayConstraint);
            }

            return delayConstraints;
        }

        private static void FindStimulusResponses
        (
            IEnumerable<XElement> eventChains,
            IEnumerable<XElement> eventFunctions,
            string scopeRef,
            ref List<StimulusResponse> stimulusResponses
        )
        { 
            if (string.IsNullOrEmpty(scopeRef))
            {
                return;
            }

            var resourceParent = eventChains.Single(x => x.Name.LocalName == "SHORT-NAME" && x.Value == scopeRef).Parent;
            var stimulusPortName = FindPortName(resourceParent, eventFunctions, "STIMULUS-REF");
            var responsePortName = FindPortName(resourceParent, eventFunctions, "RESPONSE-REF");

            StimulusResponse stimulusResponse = new StimulusResponse()
            {
                Stimulus = stimulusPortName,
                Response = responsePortName
            };

            stimulusResponses.Add(stimulusResponse);
            
            var segRefs = resourceParent.Elements()
                           .First(c => c.Name.LocalName == "SEGMENT-REFS");

            var seg = "";
            if (segRefs.HasElements)
            {

                 seg = FindResourceName(segRefs.GetChildValueByLocalName("SEGMENT-REF"));
            }

            FindStimulusResponses(eventChains, eventFunctions, seg, ref stimulusResponses);
        }

        private static void AFindStimulusResponses
        (
            IEnumerable<XElement> eventChains,
            IEnumerable<XElement> eventFunctions,
            string scopeRef,
            ref List<StimulusResponse> stimulusResponses
        )
        {
            if (string.IsNullOrEmpty(scopeRef))
            {
                return;
            }

            var resourceParent = eventChains.Single(x => x.Name.LocalName == "SHORT-NAME" && x.Value == scopeRef).Parent;
            var stimulusPortName = FindPortName(resourceParent, eventFunctions, "STIMULUS-REF");
            var responsePortName = FindPortName(resourceParent, eventFunctions, "RESPONSE-REF");

            StimulusResponse stimulusResponse = new StimulusResponse()
            {
                Stimulus = stimulusPortName,
                Response = responsePortName
            };

            stimulusResponses.Add(stimulusResponse);

            var segRefs = resourceParent.Elements()
                           .First(c => c.Name.LocalName == "SEGMENT-REFS");

            if (segRefs.HasElements)
            {
                foreach (var segmentRef in segRefs.GetChildrenValueByLocalName("SEGMENT-REF"))
                {
                    var seg = FindResourceName(segmentRef);

                    FindStimulusResponses(eventChains, eventFunctions, seg, ref stimulusResponses);
                }
            }


        }

        private static string FindPortName(XElement resourceParent, IEnumerable<XElement> eventFunctions, string resourceLocalName)
        {
            var resourceValue = resourceParent.GetChildValueByLocalName(resourceLocalName);

            var resourceRef = FindResourceName(resourceValue);

            var flowPortNode = eventFunctions.Single(x => x.Name.LocalName == "SHORT-NAME" && x.Value == resourceRef).Parent
                .Elements()
                .Single(c => c.Name.LocalName == "PORT-IREF")
                .GetChildValueByLocalName("FUNCTION-FLOW-PORT-REF");

            var portName = FindResourceName(flowPortNode);

            return portName;
        }


        private static string FindResourceName(string resourcePath)
        {
            return resourcePath.Split('/').Last();
        }
    }
}
