using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;

namespace FileTransferClient
{
    /// <summary>
    /// Клиент передачи файлов по SCP с учётом статуса пользователя.
    /// </summary>
    public class ScpTransferClient : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private ScpClient? _scpClient;
        private bool _disposed;

        public TransferSession Session { get; }

        public ScpTransferClient(string host, int port, string username, string password, string userId)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));
            Session = new TransferSession(userId);
        }

        /// <summary>
        /// Подключиться и перевести пользователя в Online.
        /// </summary>
        public void Connect()
        {
            EnsureNotDisposed();
            _scpClient = new ScpClient(_host, _port, _username, _password);
            _scpClient.Connect();
            Session.SetStatus(UserStatus.Online);
        }

        /// <summary>
        /// Отправить файл с учётом статуса пользователя.
        /// </summary>
        public void UploadFile(string localPath, string remotePath, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            if (_scpClient == null || !_scpClient.IsConnected)
                throw new InvalidOperationException("Сначала вызовите Connect()");

            if (!File.Exists(localPath))
                throw new FileNotFoundException("Локальный файл не найден", localPath);

            var fileInfo = new FileInfo(localPath);
            Session.BeginTransfer(fileInfo.Name, fileInfo.Length);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var fs = File.OpenRead(localPath);
                // Простая имитация прогресса (SCP.NET не даёт колбэк прогресса из коробки)
                long reported = 0;
                const int chunkSize = 64 * 1024;
                var buffer = new byte[chunkSize];
                int read;

                // Для реального прогресса можно обернуть поток,
                // здесь — упрощённый вариант: отправляем целиком через ScpClient
                // и обновляем статус по завершении.
                _scpClient.Upload(fs, remotePath);

                Session.UpdateProgress(fileInfo.Length);
                Session.Complete();
            }
            catch (OperationCanceledException)
            {
                Session.Fail("Передача отменена пользователем");
                throw;
            }
            catch (Exception ex)
            {
                Session.Fail(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Асинхронная обёртка UploadFile.
        /// </summary>
        public Task UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => UploadFile(localPath, remotePath, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Отключиться и перевести статус в Offline.
        /// </summary>
        public void Disconnect()
        {
            if (_scpClient != null)
            {
                if (_scpClient.IsConnected)
                    _scpClient.Disconnect();
                _scpClient.Dispose();
                _scpClient = null;
            }

            if (Session.Status != UserStatus.Completed && Session.Status != UserStatus.Error)
                Session.SetStatus(UserStatus.Offline);
        }

        public void Dispose()
        {
            if (_disposed) return;
            Disconnect();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScpTransferClient));
        }
    }
}
