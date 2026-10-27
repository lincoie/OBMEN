using System;
using Xunit;
using FileTransferClient;

namespace FileTransferClient.Tests
{
    public class TransferSessionTests
    {
        [Fact]
        public void Constructor_SetsInitialState()
        {
            var session = new TransferSession("user1");

            Assert.Equal("user1", session.UserId);
            Assert.Equal(UserStatus.Offline, session.Status);
            Assert.Null(session.CurrentFile);
            Assert.Equal(0, session.BytesTransferred);
            Assert.Equal(0, session.TotalBytes);
            Assert.Null(session.StartedAt);
            Assert.Null(session.FinishedAt);
        }

        [Fact]
        public void Constructor_EmptyUserId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new TransferSession(""));
            Assert.Throws<ArgumentException>(() => new TransferSession("   "));
        }

        [Fact]
        public void SetStatus_Online_FromOffline_Works()
        {
            var session = new TransferSession("user1");
            var changed = session.SetStatus(UserStatus.Online);

            Assert.True(changed);
            Assert.Equal(UserStatus.Online, session.Status);
        }

        [Fact]
        public void SetStatus_SameStatus_ReturnsFalse()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            var changed = session.SetStatus(UserStatus.Online);

            Assert.False(changed);
        }

        [Fact]
        public void BeginTransfer_FromOnline_SetsTransferring()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);

            session.BeginTransfer("report.pdf", 1024);

            Assert.Equal(UserStatus.Transferring, session.Status);
            Assert.Equal("report.pdf", session.CurrentFile);
            Assert.Equal(1024, session.TotalBytes);
            Assert.Equal(0, session.BytesTransferred);
            Assert.NotNull(session.StartedAt);
        }

        [Fact]
        public void BeginTransfer_FromOffline_Throws()
        {
            var session = new TransferSession("user1");

            Assert.Throws<InvalidOperationException>(() =>
                session.BeginTransfer("file.txt", 100));
        }

        [Fact]
        public void UpdateProgress_Valid_UpdatesBytes()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("file.bin", 1000);

            session.UpdateProgress(400);

            Assert.Equal(400, session.BytesTransferred);
            Assert.Equal(40.0, session.ProgressPercent, 1);
        }

        [Fact]
        public void UpdateProgress_OutOfRange_Throws()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("file.bin", 1000);

            Assert.Throws<ArgumentOutOfRangeException>(() => session.UpdateProgress(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.UpdateProgress(1001));
        }

        [Fact]
        public void Pause_And_Resume_Works()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("file.bin", 500);

            session.Pause();
            Assert.Equal(UserStatus.Paused, session.Status);

            session.Resume();
            Assert.Equal(UserStatus.Transferring, session.Status);
        }

        [Fact]
        public void Complete_SetsCompletedAndProgress100()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("file.bin", 200);
            session.UpdateProgress(150);

            session.Complete();

            Assert.Equal(UserStatus.Completed, session.Status);
            Assert.Equal(200, session.BytesTransferred);
            Assert.Equal(100.0, session.ProgressPercent);
            Assert.NotNull(session.FinishedAt);
        }

        [Fact]
        public void Fail_SetsErrorStatusAndMessage()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("file.bin", 100);

            session.Fail("Сеть недоступна");

            Assert.Equal(UserStatus.Error, session.Status);
            Assert.Equal("Сеть недоступна", session.LastError);
            Assert.NotNull(session.FinishedAt);
        }

        [Fact]
        public void StatusChanged_Event_IsRaised()
        {
            var session = new TransferSession("user1");
            UserStatus? received = null;
            session.StatusChanged += s => received = s;

            session.SetStatus(UserStatus.Online);

            Assert.Equal(UserStatus.Online, received);
        }

        [Fact]
        public void Cannot_StartTransfer_AfterCompleted()
        {
            var session = new TransferSession("user1");
            session.SetStatus(UserStatus.Online);
            session.BeginTransfer("a.txt", 10);
            session.Complete();

            Assert.Throws<InvalidOperationException>(() =>
                session.SetStatus(UserStatus.Transferring));
        }
    }
}
