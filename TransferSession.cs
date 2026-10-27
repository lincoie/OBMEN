using System;

namespace FileTransferClient
{
    /// <summary>
    /// Сессия передачи с учётом статуса пользователя.
    /// </summary>
    public class TransferSession
    {
        public string UserId { get; }
        public UserStatus Status { get; private set; }
        public string? CurrentFile { get; private set; }
        public long BytesTransferred { get; private set; }
        public long TotalBytes { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        public string? LastError { get; private set; }

        public event Action<UserStatus>? StatusChanged;

        public TransferSession(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId не может быть пустым", nameof(userId));

            UserId = userId;
            Status = UserStatus.Offline;
        }

        /// <summary>
        /// Установить статус пользователя. Возвращает true, если статус изменился.
        /// </summary>
        public bool SetStatus(UserStatus newStatus)
        {
            if (Status == newStatus)
                return false;

            // Простые правила переходов
            if (Status == UserStatus.Offline && newStatus == UserStatus.Transferring)
                throw new InvalidOperationException("Нельзя начать передачу из статуса Offline. Сначала Online.");

            if (Status == UserStatus.Completed && newStatus == UserStatus.Transferring)
                throw new InvalidOperationException("Сессия уже завершена. Создайте новую.");

            Status = newStatus;

            if (newStatus == UserStatus.Transferring && StartedAt == null)
                StartedAt = DateTime.UtcNow;

            if (newStatus == UserStatus.Completed || newStatus == UserStatus.Error)
                FinishedAt = DateTime.UtcNow;

            StatusChanged?.Invoke(Status);
            return true;
        }

        public void BeginTransfer(string fileName, long totalBytes)
        {
            if (Status != UserStatus.Online && Status != UserStatus.Paused)
                throw new InvalidOperationException($"Нельзя начать передачу в статусе {Status}");

            CurrentFile = fileName;
            TotalBytes = totalBytes;
            BytesTransferred = 0;
            LastError = null;
            SetStatus(UserStatus.Transferring);
        }

        public void UpdateProgress(long bytesTransferred)
        {
            if (Status != UserStatus.Transferring)
                throw new InvalidOperationException("Прогресс можно обновлять только во время передачи");

            if (bytesTransferred < 0 || bytesTransferred > TotalBytes)
                throw new ArgumentOutOfRangeException(nameof(bytesTransferred));

            BytesTransferred = bytesTransferred;
        }

        public void Pause()
        {
            if (Status != UserStatus.Transferring)
                throw new InvalidOperationException("Пауза возможна только во время передачи");

            SetStatus(UserStatus.Paused);
        }

        public void Resume()
        {
            if (Status != UserStatus.Paused)
                throw new InvalidOperationException("Возобновление возможно только из паузы");

            SetStatus(UserStatus.Transferring);
        }

        public void Complete()
        {
            if (Status != UserStatus.Transferring && Status != UserStatus.Paused)
                throw new InvalidOperationException("Завершить можно только активную или приостановленную передачу");

            BytesTransferred = TotalBytes;
            SetStatus(UserStatus.Completed);
        }

        public void Fail(string errorMessage)
        {
            LastError = errorMessage ?? "Неизвестная ошибка";
            SetStatus(UserStatus.Error);
        }

        public double ProgressPercent =>
            TotalBytes == 0 ? 0 : (double)BytesTransferred / TotalBytes * 100.0;
    }
}
