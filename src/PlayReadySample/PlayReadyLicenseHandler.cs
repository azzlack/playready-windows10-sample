namespace PlayReadySample
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Windows.Media.Protection.PlayReady;

    public class PlayReadyLicenseHandler
    {
        /// <summary>Request a token that identifies the player session.</summary>
        /// <param name="request">The request.</param>
        /// <returns><c>True</c> if successfull, <c>false</c> otherwise.</returns>
        public static async Task<bool> RequestIndividualizationToken(PlayReadyIndividualizationServiceRequest request)
        {
            Debug.WriteLine("ProtectionManager PlayReady Individualization Service Request in progress");

            try
            {
                Debug.WriteLine("Requesting individualization token from {0}", request.Uri);

                await request.BeginServiceRequest();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ProtectionManager PlayReady Individualization Service Request failed: " + ex.Message);

                return false;
            }

            Debug.WriteLine("ProtectionManager PlayReady Individualization Service Request successfull");

            return true;
        }

        /// <summary>Request a license for playing a stream.</summary>
        /// <param name="request">The request.</param>
        /// <returns><c>True</c> if successfull, <c>false</c> otherwise.</returns>
        public static async Task<bool> RequestLicense(PlayReadyLicenseAcquisitionServiceRequest request)
        {
            Debug.WriteLine("ProtectionManager PlayReady License Request in progress");

            try
            {
                Debug.WriteLine("Requesting license from {0} with custom data {1}", request.Uri, request.ChallengeCustomData);

                await request.BeginServiceRequest();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ProtectionManager PlayReady License Request failed: " + ex.Message);

                return false;
            }

            Debug.WriteLine("ProtectionManager PlayReady License Request successfull");

            return true;
        }

        public static async Task<bool> RequestLicenseManual(PlayReadyLicenseAcquisitionServiceRequest request)
        {
            Debug.WriteLine("ProtectionManager PlayReady Manual License Request in progress");

            try
            {
                var r = request.GenerateManualEnablingChallenge();

                var content = new ByteArrayContent(r.GetMessageBody());

                foreach (var header in r.MessageHeaders.Where(x => x.Value != null))
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value.ToString());
                    }
                    else
                    {
                        content.Headers.Add(header.Key, header.Value.ToString());
                    }
                }

                var msg = new HttpRequestMessage(HttpMethod.Post, r.Uri) { Content = content };

                Debug.WriteLine("Requesting license from {0} with custom data {1}", msg.RequestUri, await msg.Content.ReadAsStringAsync());

                var client = new HttpClient();
                var response = await client.SendAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    request.ProcessManualEnablingResponse(await response.Content.ReadAsByteArrayAsync());
                }
                else
                {
                    Debug.WriteLine("ProtectionManager PlayReady License Request failed: " + await response.Content.ReadAsStringAsync());

                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ProtectionManager PlayReady License Request failed: " + ex.Message);

                return false;
            }

            Debug.WriteLine("ProtectionManager PlayReady License Request successfull");

            return true;
        }
    }
}