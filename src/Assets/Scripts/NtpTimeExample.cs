using System;
using UnityEngine;


namespace HassakuLab.NtpClients.Example
{
    public class NtpTimeExample : MonoBehaviour
    {
        [SerializeField] private NtpTime ntp;
        
        // Start is called before the first frame update
        private async void Start()
        {
            try
            {
                await ntp.Sync();
            }
            catch (Exception _)
            {
                Debug.Log("catch exception");
                throw;
            }

            Debug.Log(ntp.Now);
        }
    }

}