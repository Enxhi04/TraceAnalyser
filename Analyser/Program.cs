using Analyser.Constraints;
using Analyser.VerificationValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    class Program
    {
        private const string timeStamp = "timestamp";
        private const double DEFAULT_TIMESTAMP_SCALE = 1;

        static void Main(string[] args)
        {
            XNamespace ns = "http://east-adl.info/2.1.12";
            var constraintsProvider = new ConstraintsProvider("./Files/BasicWW4.eaxml", ns.NamespaceName);

            var validationWriter = new ValidationWriter("./Files/BasicWW4.eaxml", ns);

            var allDelayConstraints = constraintsProvider.GetAllDelayConstraints();
            var allAgeConstraints = constraintsProvider.GetAllAgeConstraints();
            var allReactionConstraints = constraintsProvider.GetAllReactionConstraints();

            var logStream = new LogStream("./Files/LogFileReactionConstaints.csv", timeStamp, args.Any() ? double.Parse(args[0]) : DEFAULT_TIMESTAMP_SCALE);

            var log = logStream.GetNextLog();
            //first log is always enqueued
            logStream.QueuedLogs.Enqueue(log);

            log = logStream.GetNextLog();
            while (log != null)
            {
                var changedSignals = log.ChangedColumns;

                foreach (var responseSignal in changedSignals)
                {
                    // if log contains R1 it can be validated because it is the last signal that can be triggered in a chain of stiluli and response signals[{ s1,r1}, { s2,r2}, .... { sn,rn}]
                    var validatableAgeConstaints = allAgeConstraints
                        .Where(x => x.StimulusResponses.First().Response == responseSignal)
                        .ToList();
                    foreach (var constraint in validatableAgeConstaints)
                    {
                       ValidateAgeConstraint(constraint, log, logStream, validationWriter);
                    }

                    var validatableReactionConstaints = allReactionConstraints
                        .Where(x => x.InitialStimulusResponse.Response == responseSignal)
                        .ToList();
                    foreach (var constraint in validatableReactionConstaints)
                    {
                        ValidateReactionConstraint(constraint, log, logStream, validationWriter);
                    }

                    var validatableDelayConstraints = allDelayConstraints
                        .Where(x => x.StimulusResponse.Response == responseSignal)
                        .ToList();

                    foreach (var constraint in validatableDelayConstraints)
                    {
                        ValidateDelayConstraint(constraint, log, logStream, validationWriter);
                    }
                }

                logStream.QueuedLogs.Enqueue(log);

                DeleteFromQueue(allAgeConstraints, allDelayConstraints, allReactionConstraints, logStream);
                log = logStream.GetNextLog();
            }

            validationWriter.Save();
        }

        private static void ValidateAgeConstraint(AgeConstraint constraint, Log log, LogStream logStream, ValidationWriter validationWriter)
        {
            // needed to check the order of the signals. stimulus1<stimulus2...<stimulusN-1<stimulusN<responseN<responseN-1...<response2<response1 
            double lastSignalTimestamp = Double.MinValue;
            var possibleChainTimestamps = new List<string>();

            var stimuli = constraint.StimulusResponses.Select(x => x.Stimulus).ToList();
            if (!ValidateSignals(constraint, logStream, stimuli, ref lastSignalTimestamp, ref possibleChainTimestamps))
            {
                return;
            }
            // skip initial response 
            var responses = constraint.StimulusResponses.SkipLast(1).Select(x => x.Response).Reverse().ToList();
            if (!ValidateSignals(constraint, logStream, responses, ref lastSignalTimestamp, ref possibleChainTimestamps))
            {
                return;
            }

            constraint.ProccessedStimulusTimestamps.AddRange(possibleChainTimestamps);
            double firstStimulusTimestamp = double.Parse(possibleChainTimestamps.First());
            // validate constraint
            if (double.Parse(log[timeStamp]) - firstStimulusTimestamp != constraint.Value)
            {

                Console.WriteLine
                (
                    $"Stimulus signal {constraint.StimulusResponses.First().Stimulus}:{firstStimulusTimestamp} " +
                    $"with response signal {constraint.StimulusResponses.First().Response}:{log[timeStamp]} " +
                    $"failed for AGE-CONSTRAINT with NAME {constraint.Value}."
                );

                var vvLog = new VerificationValidationLog()
                {
                    Constraint = constraint.ShortName,
                    ConstraintType = "AGE-CONSTRAINT",
                    Response = constraint.StimulusResponses.First().Response,
                    ResponseTimestamp = log[timeStamp],
                    Stimulus = constraint.StimulusResponses.First().Stimulus,
                    StimulusTimestamp = firstStimulusTimestamp.ToString(),
                    Value = constraint.Value
                };

                validationWriter.WriteValidation(vvLog);


            }
        }

        private static void ValidateReactionConstraint(ReactionConstraint constraint, Log log, LogStream logStream, ValidationWriter validationWriter)
        {
            // needed to check the order of the signals. stimulus1<stimulus2...<stimulusN-1<stimulusN<responseN<responseN-1...<response2<response1 
            double lastSignalTimestamp = Double.MinValue;
            var possibleChainTimestamps = new List<string>();

            if (!ValidateSignals(
                constraint,
                logStream,
                new List<string>() { constraint.InitialStimulusResponse.Stimulus },
                ref lastSignalTimestamp,
                ref possibleChainTimestamps)
             )
            {
                return;
            }

            for (int i = 0; i < constraint.StimulusResponses.Count; i++)
            {
                var currentSegmentStimulusResponses = constraint.StimulusResponses[i];

                List<string> stimuli;
                // when first stimulus of the segment is the same as last response of previous segment we count the same change for both signals
                if (i == 0 || currentSegmentStimulusResponses.First().Stimulus != constraint.StimulusResponses[i - 1].Last().Response)
                {
                    stimuli = currentSegmentStimulusResponses.Select(x => x.Stimulus)
                        .ToList();
                }
                else
                {
                    stimuli = currentSegmentStimulusResponses.Skip(1)
                        .Select(x => x.Stimulus)
                        .ToList();
                }

                if (!ValidateSignals(constraint, logStream, stimuli, ref lastSignalTimestamp, ref possibleChainTimestamps))
                {
                    return;
                }
                var responses = currentSegmentStimulusResponses.Select(x => x.Response).Reverse().ToList();
                if (!ValidateSignals(constraint, logStream, responses, ref lastSignalTimestamp, ref possibleChainTimestamps))
                {
                    return;
                }
            }

            // skiping initial response

            constraint.ProccessedStimulusTimestamps.AddRange(possibleChainTimestamps);
            double firstStimulusTimestamp = double.Parse(possibleChainTimestamps.First());
            // validate constraint
            if (double.Parse(log[timeStamp]) - firstStimulusTimestamp != constraint.Value)
            {

                Console.WriteLine
                (
                    $"Stimulus signal {constraint.InitialStimulusResponse.Stimulus}:{firstStimulusTimestamp} " +
                    $"with response signal {constraint.InitialStimulusResponse.Response}:{log[timeStamp]} " +
                    $"failed for REACTION-CONSTRAINT with NAME {constraint.Value}."
                );

                var vvLog = new VerificationValidationLog()
                {
                    Constraint = constraint.ShortName,
                    ConstraintType = "REACTION-CONSTRAINT",
                    Response = constraint.InitialStimulusResponse.Response,
                    ResponseTimestamp = log[timeStamp],
                    Stimulus = constraint.InitialStimulusResponse.Stimulus,
                    StimulusTimestamp = firstStimulusTimestamp.ToString(),
                    Value = constraint.Value
                };

                validationWriter.WriteValidation(vvLog);
            }
        }

        private static bool ValidateSignals(
            Constraint constraint,
            LogStream logStream,
            List<string> signals,
            ref double lastSignalTimestamp,
            ref List<string> possibleChainTimestamps
        )
        {
            var previousSignalTimestamp = lastSignalTimestamp;
            foreach (var signal in signals)
            {

                // first stimulus log that is not processed by this constraint
                var stimulusLog = logStream.GetChangedLogs(signal)
                    .FirstOrDefault(l => double.Parse(l[timeStamp]) > previousSignalTimestamp && !constraint.ProccessedStimulusTimestamps.Contains(l[timeStamp]));

                if (stimulusLog == null)
                {
                    //there is a response change, but not a stimulus chain therefore the signal has changed because of something else
                    return false;
                }

                previousSignalTimestamp = double.Parse(stimulusLog[timeStamp]);
                possibleChainTimestamps.Add(stimulusLog[timeStamp]);
            }

            lastSignalTimestamp = previousSignalTimestamp;

            return true;
        }

        private static void ValidateDelayConstraint(DelayConstraint constraint, Log log, LogStream logStream, ValidationWriter validationWriter)
        {
            var stimulusSignalName = constraint.StimulusResponse.Stimulus;

            // first stimulus log that is not processed by this constraint
            var stimulusLog = logStream.GetChangedLogs(stimulusSignalName)
                .FirstOrDefault(l => !constraint.ProccessedStimulusTimestamps.Contains(l[timeStamp]));

            if (stimulusLog == null)
            {
                //there is a response change, but not a stimulus therefore the signal has changed because of something else
                return;
            }
            constraint.ProccessedStimulusTimestamps.Add(stimulusLog[timeStamp]);
            // validate constraint
            if (double.Parse(log[timeStamp]) - double.Parse(stimulusLog[timeStamp]) != constraint.Value)
            {
                Console.WriteLine
                (
                    $"Stimulus signal {stimulusSignalName}:{stimulusLog[timeStamp]} with response signal {constraint.StimulusResponse.Response}:{log[timeStamp]} " +
                    $"failed for DELAY-CONSTRAINT with NAME {constraint.Value}."
                );

                var vvLog = new VerificationValidationLog()
                {
                    Constraint = constraint.ShortName,
                    ConstraintType = "DELAY-CONSTRAINT",
                    Response = constraint.StimulusResponse.Response,
                    ResponseTimestamp = log[timeStamp],
                    Stimulus = stimulusSignalName,
                    StimulusTimestamp = stimulusLog[timeStamp],
                    Value = constraint.Value
                };

                validationWriter.WriteValidation(vvLog);
            }
        }

        private static void DeleteFromQueue(
            List<AgeConstraint> allAgeConstraints,
            List<DelayConstraint> allDelayConstraints,
            List<ReactionConstraint> allReactionConstraints,
            LogStream logStream
        )
        {
            int logsToDelete = 0;
            foreach (var log in logStream.QueuedLogs)
            {
                if (!CanDeleteLog(log, allAgeConstraints, allDelayConstraints, allReactionConstraints))
                {
                    break;
                }
                // this means that the log does not have a stimulus 1 or has but it is already proccessed
                logsToDelete++;

                // remove from constraints 
                foreach (var ageConstraint in allAgeConstraints)
                {
                    ageConstraint.ProccessedStimulusTimestamps.Remove(log[timeStamp]);
                }

                foreach (var delayConstraint in allDelayConstraints)
                {
                    delayConstraint.ProccessedStimulusTimestamps.Remove(log[timeStamp]);
                }
            }

            // delete not needed logs
            for (int i = 0; i < logsToDelete; i++)
            {
                logStream.QueuedLogs.Dequeue();
            }
        }

        private static bool CanDeleteLog(
            Log log,
            List<AgeConstraint> allAgeConstraints,
            List<DelayConstraint> allDelayConstraints,
            List<ReactionConstraint> allReactionConstraints
        )
        {
            foreach (var ageConstraint in allAgeConstraints)
            {
                bool isStimulus = log.ChangedColumns.Contains(ageConstraint.StimulusResponses.First().Stimulus);
                //is log has a non processed stimulus
                if (isStimulus && !ageConstraint.ProccessedStimulusTimestamps.Contains(log[timeStamp]))
                {
                    return false;
                }
            }

            foreach (var delayConstraint in allDelayConstraints)
            {
                bool isStimulus = log.ChangedColumns.Contains(delayConstraint.StimulusResponse.Stimulus);
                //is log has a non processed stimulus
                if (isStimulus && !delayConstraint.ProccessedStimulusTimestamps.Contains(log[timeStamp]))
                {
                    return false;
                }
            }

            foreach (var reactionConstraint in allReactionConstraints)
            {
                bool isStimulus = log.ChangedColumns.Contains(reactionConstraint.InitialStimulusResponse.Stimulus);
                //is log has a non processed stimulus
                if (isStimulus && !reactionConstraint.ProccessedStimulusTimestamps.Contains(log[timeStamp]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}