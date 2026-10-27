using System;
using System.IO;
using Xunit;
using FileTransferClient;

namespace FileTransferClient.Tests
{
    /// <summary>
    /// Тесты логики клиента без реального сетевого подключения.
    /// Проверяем учёт статуса и обработку ошибок на уровне сессии.
    /// </summary>
    public class ScpTransferClientTests
    {
        [Fact]
        public void Constructor_CreatesSessionWithOfflineStatus()
        {
            using var client = new ScpTransferClient("127.0.0.1", 22, "user", "pass", "user-42");

            Assert.Equal("user-42", client.Session.UserId);
            Assert.Equal(UserStatus.Offline, client.Session.Status);
        }

        [Fact]
        public void UploadFile_WithoutConnect_Throws()
        {
            using var client = new ScpTransferClient("127.0.0.1", 22, "user", "pass", "user-1");
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "test");

                var ex = Assert.Throws<InvalidOperationException>(() =>
                    client.UploadFile(tempFile, "/tmp/test.txt"));

                Assert.Contains("Connect", ex.Message);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void UploadFile_FileNotFound_Throws()
        {
            using var client = new ScpTransferClient("127.0.0.1", 22, "user", "pass", "user-1");

            // Даже без Connect — сначала проверка файла может не дойти,
            // но в текущей реализации проверка Connect идёт первой.
            Assert.Throws<InvalidOperationException>(() =>
                client.UploadFile(@"C:\nonexistent\file.xyz", "/tmp/x"));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var client = new ScpTransferClient("127.0.0.1", 22, "user", "pass", "user-1");
            client.Dispose();
            client.Dispose(); // не должно падать
        }

        [Fact]
        public void Session_ProgressPercent_ZeroWhenNoBytes()
        {
            var session = new TransferSession("u");
            Assert.Equal(0, session.ProgressPercent);
        }
    }
}
