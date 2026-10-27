namespace FileTransferClient
{
    /// <summary>
    /// Статус пользователя в контексте передачи данных.
    /// </summary>
    public enum UserStatus
    {
        /// <summary>Пользователь не в сети / недоступен</summary>
        Offline = 0,

        /// <summary>Пользователь в сети, готов к передаче</summary>
        Online = 1,

        /// <summary>Идёт передача данных</summary>
        Transferring = 2,

        /// <summary>Передача приостановлена</summary>
        Paused = 3,

        /// <summary>Передача завершена успешно</summary>
        Completed = 4,

        /// <summary>Ошибка при передаче</summary>
        Error = 5
    }
}
