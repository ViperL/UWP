using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Router
{
    class RSAConvert
    {
        public static string PublicEncrypt(string rawData)//加密字符串
        {
            try
            {
                IBuffer bufferRawData = CryptographicBuffer.ConvertStringToBinary(rawData, BinaryStringEncoding.Utf8);
                AsymmetricKeyAlgorithmProvider provider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm(AsymmetricAlgorithmNames.RsaPkcs1);
                string PUBLIC_KEY = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCJIYvV7IEzHOm4vdgu8RWc9it09ggXCKNAMEtcQM4kXT1mQyEnbEeGVYOyJWTd2jVhpToxMQ4r9p9EKd1bMClAA/KhUjQC/l9YdS3MFAZC65V47Bx6CTad4bZo1E1D6x1rOjQe4lANxWnApwY4QPYGFxEmQD3yNyErh38VlNL2GQIDAQAB";
                CryptographicKey publicKey = provider.ImportPublicKey(CryptographicBuffer.DecodeFromBase64String(PUBLIC_KEY));
                IBuffer result = CryptographicEngine.Encrypt(publicKey, bufferRawData, null);
                byte[] res;
                CryptographicBuffer.CopyToByteArray(result, out res);
                return Convert.ToBase64String(res);
            }
            catch (Exception)
            {
                return rawData;
            }
        }

        /// <summary>
        /// WPRT的RSA私钥解密
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string PrivateDecrypt(string rawData)
        {
            try
            {
                /*将文本转换成IBuffer*/
                IBuffer bufferRawData = CryptographicBuffer.ConvertStringToBinary(rawData, BinaryStringEncoding.Utf8);

                /*加密算法提供程序*/
                AsymmetricKeyAlgorithmProvider provider = AsymmetricKeyAlgorithmProvider.OpenAlgorithm
                    (AsymmetricAlgorithmNames.RsaPkcs1);

                /*导入私钥*/
                string PRIVATE_KEY = "MIICXAIBAAKBgQCJIYvV7IEzHOm4vdgu8RWc9it09ggXCKNAMEtcQM4kXT1mQyEnbEeGVYOyJWTd2jVhpToxMQ4r9p9EKd1bMClAA/KhUjQC/l9YdS3MFAZC65V47Bx6CTad4bZo1E1D6x1rOjQe4lANxWnApwY4QPYGFxEmQD3yNyErh38VlNL2GQIDAQABAoGAFxGmpZFI1uFpTCPbx2HVQfeDrgRprf5NAFJfiyB3zVRGLPrkC+7CRY4DPqfdxRidXFTgakAXYzv05RGp5FpAxf1GBAR6HQxVcntBpNqJUo2UBP+IrXgPFDPdodAl9SWgaHKwc79pCARVdJutm86kRRsy5rjcWpR8HQCYWzk/lmUCQQDcRnenz6CAhMvztS04APlly3uhvLGIVsXkGdsmv7JltRvFmZ/1MqpvAbC6WrzFMlWeFgYz6EyBiqfH6m4CbdJbAkEAn18K40E3r9WzPlbhIAEs2SiY68iZGCmzR/0n6PR48NtFSGsSDRIR632oeftBPfN7W4+kUIehL2gt9RgnRH8bmwJBAJMYK4dQSyoHg/q2nf+sBt9HRsP2sccNyxBLg+EYWhU5H9aQhBTFRLLkOhP3y98TgcETjAjVs2E+KlSB4/yTQckCQH4axVG23CptDQyp0C7z3xnh7sa7DrC45lxzK25Aa6YhyruXxUvEXZuZ7YK/1gsAKz7y9RCnkVoitCK4vvGLJjsCQBbzsW7HwZVe5RMiRsjxX7v0Ng4NnM85tnX0GUmkpuixOpfcq3PsZo7ujcBvJ5IJvbXo4QuuIRKSmxItHI26tkI=";
                CryptographicKey privateKey = provider.ImportKeyPair(CryptographicBuffer.DecodeFromBase64String(PRIVATE_KEY));

                //解密
                IBuffer result = CryptographicEngine.Decrypt(privateKey, bufferRawData, null);
                byte[] res;
                CryptographicBuffer.CopyToByteArray(result, out res);
                return Encoding.UTF8.GetString(res, 0, res.Length);
            }
            catch (Exception)
            {
                return rawData;
            }
        }
    }
}
