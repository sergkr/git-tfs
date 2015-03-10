using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class TfsWorkspaceTests
    {
        private TfsWorkspace BuildWorkspace(IWorkspace workspace, TextWriter writer)
        {
            string localDirectory = string.Empty;
            TfsChangesetInfo contextVersion = MockRepository.GenerateStub<TfsChangesetInfo>();
            IGitTfsRemote remote = MockRepository.GenerateStub<IGitTfsRemote>();
            CheckinOptions checkinOptions = StubCheckinOptions;
            ITfsHelper tfsHelper = MockRepository.GenerateStub<ITfsHelper>();
            CheckinPolicyEvaluator policyEvaluator = new CheckinPolicyEvaluator();

            return new TfsWorkspace(workspace, localDirectory, writer, contextVersion, remote, checkinOptions, tfsHelper, policyEvaluator);
        }

        private CheckinOptions StubCheckinOptions
        {
            get
            {
                var configLoader = new ConfigPropertyLoader(MockRepository.GenerateStub<Globals>());
                var configProperties = new ConfigProperties(configLoader);
                return new CheckinOptions(configProperties);
            }
        }

        [Fact]
        public void Nothing_to_checkin()
        {
            IWorkspace workspace = MockRepository.GenerateStub<IWorkspace>();
            TfsWorkspace tfsWorkspace = BuildWorkspace(workspace, new StringWriter());

            workspace.Stub(w => w.GetPendingChanges()).Return(null);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(StubCheckinOptions);
            });

            Assert.Equal("Nothing to checkin!", ex.Message);
        }

        [Fact]
        public void Checkin_failed()
        {
            IWorkspace workspace = MockRepository.GenerateStub<IWorkspace>();
            TfsWorkspace tfsWorkspace = BuildWorkspace(workspace, new StringWriter());

            IPendingChange pendingChange = MockRepository.GenerateStub<IPendingChange>();
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Stub(w => w.GetPendingChanges()).Return(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult();

            workspace.Stub(w => w.EvaluateCheckin(
                                    Arg<TfsCheckinEvaluationOptions>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything))
                    .Return(checkinEvaluationResult);

            workspace.Expect(w => w.Checkin(
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything,
                                    Arg<TfsPolicyOverrideInfo>.Is.Anything,
                                    Arg<bool>.Is.Anything))
                      .Return(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(StubCheckinOptions);
            });

            Assert.Equal("Checkin failed!", ex.Message);
        }

        [Fact]
        public void Policy_failed()
        {
            IWorkspace workspace = MockRepository.GenerateStub<IWorkspace>();
            TextWriter writer = new StringWriter();
            TfsWorkspace tfsWorkspace = BuildWorkspace(workspace, writer);

            IPendingChange pendingChange = MockRepository.GenerateStub<IPendingChange>();
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Stub(w => w.GetPendingChanges()).Return(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            workspace.Stub(w => w.EvaluateCheckin(
                                    Arg<TfsCheckinEvaluationOptions>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything))
                    .Return(checkinEvaluationResult);

            workspace.Expect(w => w.Checkin(
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything,
                                    Arg<TfsPolicyOverrideInfo>.Is.Anything,
                                    Arg<bool>.Is.Anything))
                      .Return(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(StubCheckinOptions);
            });

            Assert.Equal("No changes checked in.", ex.Message);
            Assert.Contains("[ERROR] Policy: No work items associated.", writer.ToString());
        }

        [Fact]
        public void Policy_failed_and_Force_without_an_OverrideReason()
        {
            IWorkspace workspace = MockRepository.GenerateStub<IWorkspace>();
            TextWriter writer = new StringWriter();
            TfsWorkspace tfsWorkspace = BuildWorkspace(workspace, writer);

            IPendingChange pendingChange = MockRepository.GenerateStub<IPendingChange>();
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Stub(w => w.GetPendingChanges()).Return(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            CheckinOptions checkinOptions = StubCheckinOptions;
            checkinOptions.Force = true;

            workspace.Stub(w => w.EvaluateCheckin(
                                    Arg<TfsCheckinEvaluationOptions>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything))
                    .Return(checkinEvaluationResult);

            workspace.Expect(w => w.Checkin(
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything,
                                    Arg<TfsPolicyOverrideInfo>.Is.Anything,
                                    Arg<bool>.Is.Anything))
                      .Return(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(checkinOptions);
            });

            Assert.Equal("A reason must be supplied (-f REASON) to override the policy violations.", ex.Message);
            Assert.Contains("[ERROR] Policy: No work items associated.", writer.ToString());
        }

        [Fact]
        public void Policy_failed_and_Force_with_an_OverrideReason()
        {
            IWorkspace workspace = MockRepository.GenerateStub<IWorkspace>();
            TextWriter writer = new StringWriter();
            TfsWorkspace tfsWorkspace = BuildWorkspace(workspace, writer);

            IPendingChange pendingChange = MockRepository.GenerateStub<IPendingChange>();
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Stub(w => w.GetPendingChanges()).Return(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            CheckinOptions checkinOptions = StubCheckinOptions;
            checkinOptions.Force = true;
            checkinOptions.OverrideReason = "no work items";

            workspace.Stub(w => w.EvaluateCheckin(
                                    Arg<TfsCheckinEvaluationOptions>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything))
                    .Return(checkinEvaluationResult);

            workspace.Expect(w => w.Checkin(
                                    Arg<IPendingChange[]>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<string>.Is.Anything,
                                    Arg<ICheckinNote>.Is.Anything,
                                    Arg<IEnumerable<IWorkItemCheckinInfo>>.Is.Anything,
                                    Arg<TfsPolicyOverrideInfo>.Is.Anything,
                                    Arg<bool>.Is.Anything))
                      .Return(1);

            var result = tfsWorkspace.Checkin(checkinOptions);

            Assert.Contains("[OVERRIDDEN] Policy: No work items associated.", writer.ToString());
        }
    }
}
