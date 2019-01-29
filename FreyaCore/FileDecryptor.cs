using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Freya
{
    public static class FileDecryptor
    {
        private static readonly string Key = "Foxconn0cdd3d8d-ff0e-4801-8c2d-44bdc619380f36df0262-8611-4b05-a410-7d0ac180edfbc816e5e2-310d-4c46-b4de-6fb8ac4c5deb8ee945ba-c98";

        public static bool Decrypt(string InputFile, string OutputFile)
        {
            try
            {
                if (File.Exists(InputFile))
                {
                    using (FileStream fileStreamIn = File.OpenRead(InputFile))
                    {
                        using (FileStream fileStreamOut = File.OpenWrite(OutputFile))
                        {
                            int num = (int)fileStreamIn.Length;
                            byte[] array = new byte[131072];
                            int num2 = 0;
                            byte[] fileIV = new byte[16];
                            fileStreamIn.Read(fileIV, 0, 16);
                            byte[] filePWD = new byte[16];
                            fileStreamIn.Read(filePWD, 0, 16);
                            SymmetricAlgorithm symmetricAlgorithm = GetAlgorithmFromPassword(filePWD);
                            symmetricAlgorithm.IV = fileIV;
                            int num3 = 32;
                            long num4 = -1L;
                            HashAlgorithm hashAlgorithm = SHA256.Create();
                            using (CryptoStream cryptoStreamIn = new CryptoStream(fileStreamIn, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Read))
                            {
                                using (CryptoStream cryptoStreamOut = new CryptoStream(Stream.Null, hashAlgorithm, CryptoStreamMode.Write))
                                {
                                    BinaryReader binaryReader = new BinaryReader(cryptoStreamIn);
                                    num4 = binaryReader.ReadInt64();
                                    ulong num5 = binaryReader.ReadUInt64();
                                    if (18158797384510146255UL != num5)
                                    {
                                        /*
                                        e3.a().Debug(string.Concat(new object[]
                                        {
                                    "文件被破壞！FC_TAG:",
                                    18158797384510146255UL,
                                    "====Tag:",
                                    num5
                                        }));
                                        */
                                    }
                                    long num6 = num4 / 131072L;
                                    long num7 = num4 % 131072L;
                                    int num8 = 0;
                                    int num9;
                                    while ((long)num8 < num6)
                                    {
                                        num9 = cryptoStreamIn.Read(array, 0, array.Length);
                                        fileStreamOut.Write(array, 0, num9);
                                        cryptoStreamOut.Write(array, 0, num9);
                                        num3 += num9;
                                        num2 += num9;
                                        num8++;
                                    }
                                    if (num7 > 0L)
                                    {
                                        num9 = cryptoStreamIn.Read(array, 0, (int)num7);
                                        fileStreamOut.Write(array, 0, num9);
                                        cryptoStreamOut.Write(array, 0, num9);
                                        num3 += num9;
                                        num2 += num9;
                                    }
                                    cryptoStreamOut.Flush();
                                    cryptoStreamOut.Close();
                                    fileStreamOut.Flush();
                                    fileStreamOut.Close();
                                    byte[] hash = hashAlgorithm.Hash;
                                    byte[] array4 = new byte[hashAlgorithm.HashSize / 8];
                                    num9 = cryptoStreamIn.Read(array4, 0, array4.Length);
                                    if (array4.Length != num9 || !CompareByte(array4, hash))
                                    {
                                        // e3.a().Debug("文件被破壞！檢驗兩個Byte數組不相同");
                                    }
                                    cryptoStreamIn.Flush();
                                    cryptoStreamIn.Close();
                                }
                            }
                            if ((long)num2 != num4)
                            {
                                /*
                                e3.a().Debug(string.Concat(new object[]
                                {
                            "文件大小不匹配！outValue:",
                            num2,
                            "====lSize:",
                            num4
                                }));
                                */
                            }
                            fileStreamIn.Flush();
                            fileStreamIn.Close();
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                //e3.a().Error("解密文件報錯：" + ex.Message + ex.StackTrace);
            }
            return false;
        }

        public static bool Decrypt(Stream fileStreamIn, Stream fileStreamOut)
        {
            try
            {
                fileStreamIn.Position = 0;

                int num = (int)fileStreamIn.Length;
                byte[] array = new byte[131072];
                int num2 = 0;
                byte[] fileIV = new byte[16];
                fileStreamIn.Read(fileIV, 0, 16);
                byte[] filePWD = new byte[16];
                fileStreamIn.Read(filePWD, 0, 16);
                SymmetricAlgorithm symmetricAlgorithm = GetAlgorithmFromPassword(filePWD);
                symmetricAlgorithm.IV = fileIV;
                int num3 = 32;
                long num4 = -1L;
                HashAlgorithm hashAlgorithm = SHA256.Create();
                using (CryptoStream cryptoStreamIn = new CryptoStream(fileStreamIn, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (CryptoStream cryptoStreamOut = new CryptoStream(Stream.Null, hashAlgorithm, CryptoStreamMode.Write))
                    {
                        BinaryReader binaryReader = new BinaryReader(cryptoStreamIn);
                        num4 = binaryReader.ReadInt64();
                        ulong num5 = binaryReader.ReadUInt64();
                        if (18158797384510146255UL != num5)
                        {
                            /*
                            e3.a().Debug(string.Concat(new object[]
                            {
                        "文件被破壞！FC_TAG:",
                        18158797384510146255UL,
                        "====Tag:",
                        num5
                            }));
                            */
                        }
                        long num6 = num4 / 131072L;
                        long num7 = num4 % 131072L;
                        int num8 = 0;
                        int num9;
                        while ((long)num8 < num6)
                        {
                            num9 = cryptoStreamIn.Read(array, 0, array.Length);
                            fileStreamOut.Write(array, 0, num9);
                            cryptoStreamOut.Write(array, 0, num9);
                            num3 += num9;
                            num2 += num9;
                            num8++;
                        }
                        if (num7 > 0L)
                        {
                            num9 = cryptoStreamIn.Read(array, 0, (int)num7);
                            fileStreamOut.Write(array, 0, num9);
                            cryptoStreamOut.Write(array, 0, num9);
                            num3 += num9;
                            num2 += num9;
                        }
                        cryptoStreamOut.Flush();
                        cryptoStreamOut.Close();
                        fileStreamOut.Flush();
                        //fileStreamOut.Close();
                        byte[] hash = hashAlgorithm.Hash;
                        byte[] array4 = new byte[hashAlgorithm.HashSize / 8];
                        num9 = cryptoStreamIn.Read(array4, 0, array4.Length);
                        if (array4.Length != num9 || !CompareByte(array4, hash))
                        {
                            // e3.a().Debug("文件被破壞！檢驗兩個Byte數組不相同");
                        }
                        cryptoStreamIn.Flush();
                        cryptoStreamIn.Close();
                    }
                }
                if ((long)num2 != num4)
                {
                    /*
                    e3.a().Debug(string.Concat(new object[]
                    {
                "文件大小不匹配！outValue:",
                num2,
                "====lSize:",
                num4
                    }));
                    */
                }
                fileStreamIn.Flush();
                //fileStreamIn.Close();


                return true;

            }
            catch (Exception ex)
            {
                //e3.a().Error("解密文件報錯：" + ex.Message + ex.StackTrace);
            }
            return false;
        }


        private static SymmetricAlgorithm GetAlgorithmFromPassword(byte[] A_0)
        {
            PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(Key, A_0, "SHA256", 1000);
            SymmetricAlgorithm symmetricAlgorithm = Rijndael.Create();
            symmetricAlgorithm.KeySize = 256;
            symmetricAlgorithm.Key = passwordDeriveBytes.GetBytes(32);
            symmetricAlgorithm.Padding = PaddingMode.PKCS7;
            return symmetricAlgorithm;
        }

        private static bool CompareByte(byte[] A_0, byte[] A_1)
        {
            bool result;
            if (A_0.Length == A_1.Length)
            {
                for (int i = 0; i < A_0.Length; i++)
                {
                    if (A_0[i] != A_1[i])
                    {
                        return false;
                    }
                }
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }
    }
}
