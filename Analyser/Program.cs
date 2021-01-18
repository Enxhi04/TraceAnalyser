using Analyser.Constraints;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    class Program
    {
        private const string timeStamp = "timestamp";
        static void Main(string[] args)
        {
            var constraintsProvider = new ConstraintsProvider("./Files/BasicWW2.eaxml", "http://east-adl.info/2.1.12");

            var allDelayConstraints = constraintsProvider.GetAllDelayConstraints();
            var allAgeConstraints = constraintsProvider.GetAllAgeConstraints();

            var logStream = new LogStream("./Files/LogFile1.csv", timeStamp);

            var log = logStream.GetNextLog();
            //first log is always enqueued
            logStream.QueuedLogs.Enqueue(log);

            log = logStream.GetNextLog();
            while (log != null)
            {
                var changedSignals = log.ChangedColumns;

                foreach (var responseSignal in changedSignals)
                {// if log contains R1 it can be validated bcause it is the last signal that can be triggered in a chain of stiluli and response signals[{s1,r1}, {s2,r2}, .... {sn,rn}]
                    var validatableAgeConstaints = allAgeConstraints
                        .Where(x => x.StimulusResponses.First().Response == responseSignal) 
                        .ToList();
                    foreach (var constraint in validatableAgeConstaints)
                    {
                        ValidateAgeConstraint(constraint, log, logStream);
                    }

                    var validatableDelayConstraints = allDelayConstraints
                        .Where(x => x.StimulusResponse.Response == responseSignal)
                        .ToList();

                    foreach (var constraint in validatableDelayConstraints)
                    {
                        //if (log[timeStamp] == "305") test reason
                        //{

                        //}
                        ValidateDelayConstraint(constraint, log, logStream);
                    }
                }

                logStream.QueuedLogs.Enqueue(log);

                DeleteFromQueue(allAgeConstraints, allDelayConstraints, logStream);
                log = logStream.GetNextLog();
            }
        }

        private static void ValidateAgeConstraint(AgeConstraint constraint, Log log, LogStream logStream)
        {
            // needed to check the order of the signals. stimulus1<stimulus2...<stimulusN-1<stimulusN<responseN<responseN-1...<response2<response1 
            double lastSignalTimestamp = Double.MinValue;
            var possibleChainTimestamps = new List<string>();
            for (int i = 0; i < constraint.StimulusResponses.Count; i++)
            {
                var stimulusSignalName = constraint.StimulusResponses[i].Stimulus;

                // first stimulus log that is not processed by this constraint
                var stimulusLog = logStream.GetChangedLogs(stimulusSignalName)
                    .FirstOrDefault(l => double.Parse(l[timeStamp]) > lastSignalTimestamp && !constraint.ProccessedStimulusTimestamps.Contains(l[timeStamp]));

                if (stimulusLog == null)
                {
                    //there is a response change, but not a stimulus chain therefore the signal has changed because of something else
                    return;
                }

                lastSignalTimestamp = double.Parse(stimulusLog[timeStamp]);
                possibleChainTimestamps.Add(stimulusLog[timeStamp]);

            }

            for (int i = constraint.StimulusResponses.Count - 1; i > 0; i--)
            {
                var responseSignalName = constraint.StimulusResponses[i].Response;

                // first response log that is not processed by this constraint
                var responseLog = logStream.GetChangedLogs(responseSignalName)
                    .FirstOrDefault(l => double.Parse(l[timeStamp]) > lastSignalTimestamp && !constraint.ProccessedStimulusTimestamps.Contains(l[timeStamp]));

                if (responseLog == null)
                {
                    //there is a response change, but not a stimulus-response chain therefore the signal has changed because of something else
                    return;
                }

                lastSignalTimestamp = double.Parse(responseLog[timeStamp]);
                possibleChainTimestamps.Add(responseLog[timeStamp]);

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
            }
        }

        private static void ValidateDelayConstraint(DelayConstraint constraint, Log log, LogStream logStream)
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
            }
        }

        private static void DeleteFromQueue(List<AgeConstraint> allAgeConstraints, List<DelayConstraint> allDelayConstraints, LogStream logStream)
        {
            int logsToDelete = 0;
            foreach (var log in logStream.QueuedLogs)
            {
                if (!CanDeleteLog(log, allAgeConstraints, allDelayConstraints))
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

        private static bool CanDeleteLog(Log log, List<AgeConstraint> allAgeConstraints, List<DelayConstraint> allDelayConstraints)
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

            return true;
        }
    }
}