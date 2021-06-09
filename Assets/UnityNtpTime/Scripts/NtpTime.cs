using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HassakuLab.NtpTimes
{
    /// <summary>
    /// Component provide real now time
    /// </summary>
    [CreateAssetMenu(fileName = "NtpTime", menuName = "HassakuLab/NtpTime", order = 0)]
    public class NtpTime : ScriptableObject
    {
        /// <summary>
        /// NTP Server Address
        /// </summary>
        [SerializeField] private string ntpServerAddress;

        /// <summary>
        /// If timeoutSec > 0, set timeout [sec]
        /// </summary>
        [SerializeField] private int timeoutSec = 5;
        
        /// <summary>
        /// If true, do Sync() in Awake().
        /// </summary>
        [SerializeField] private bool syncInAwake;

        /// <summary>
        /// Get timer is synced or not.
        /// <returns>synced: true, not synced yet: false</returns>
        /// </summary>
        public bool IsSynced { get; private set; } = false;

        /// <summary>
        /// Get time last synchronized
        /// </summary>
        public DateTime LastSyncTime { get; private set; }

        private float syncTimeSinceStartup;

        /// <summary>
        /// Set and Get NTP Server Address
        /// </summary>
        public string ServerAddress
        {
            get => ntpServerAddress;
            set => ntpServerAddress = value;
        }

        /// <summary>
        /// Set socket timeout [sec]
        /// </summary>
        public int TimeOutSec
        {
            set => timeoutSec = value;
        }

        private void Awake()
        {
            if (syncInAwake)
            {
                StartSync();
            }
        }

        /// <summary>
        /// Start sync (fire and forget)
        /// </summary>
        public void StartSync()
        {
            Sync().Forget();
        }

        /// <summary>
        /// Synchronize time with NTP server coroutine
        /// </summary>
        /// <returns></returns>
        public async UniTask Sync()
        {
            LastSyncTime = await UniTask.Run(() =>
            {
                var task = NtpRequest.Request(ntpServerAddress, timeoutSec);
                return task.Result;
            });
            
            syncTimeSinceStartup = Time.realtimeSinceStartup;
            IsSynced = true;
        }

        /// <summary>
        /// Get now time
        /// </summary>
        /// <returns>now time</returns>
        public DateTime Now => LastSyncTime + TimeSpan.FromSeconds(Time.realtimeSinceStartup - syncTimeSinceStartup);
    }
}