using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using WinSCP;

namespace UploadAndLinkCmd
{
	class Program
	{
		private static string baseUploadPath;
		private static string baseUrl;
		private static string fileDir = "";
		private static string ssDir = "";

		private static SessionOptions sessionOptions = new SessionOptions();

		static void LoadConfig(string configFilePath)
		{
			var config = JObject.Parse(File.ReadAllText(configFilePath));

			if (config.ContainsKey("baseUploadPath"))
				baseUploadPath = config.Value<string>("baseUploadPath");
			else
				throw new InvalidOperationException("No key 'baseUploadPath' in server-config.json");

			if (config.ContainsKey("baseUrl"))
				baseUrl = config.Value<string>("baseUrl");
			else
				throw new InvalidOperationException("No key 'baseUrl' in server-config.json");

			if (config.ContainsKey("fileDir"))
				fileDir = config.Value<string>("fileDir");
			if (config.ContainsKey("ssDir"))
				ssDir = config.Value<string>("ssDir");

			if (config.ContainsKey("Protocol"))
				sessionOptions.Protocol = (Protocol)Enum.Parse(typeof(Protocol), config.Value<string>("Protocol"));
			else
				throw new InvalidOperationException("No key 'Protocol' in server-config.json.  Likely you should include 'Protocol':'Sftp' or 'Protocol':'Ftp'");

			if (config.ContainsKey("HostName"))
				sessionOptions.HostName = config.Value<string>("HostName");

			if (config.ContainsKey("UserName"))
				sessionOptions.UserName = config.Value<string>("UserName");

			if (config.ContainsKey("SshHostKeyFingerprint"))
				sessionOptions.SshHostKeyFingerprint = config.Value<string>("SshHostKeyFingerprint");

			if (config.ContainsKey("SshPrivateKeyPath"))
				sessionOptions.SshPrivateKeyPath = config.Value<string>("SshPrivateKeyPath");

			if (config.ContainsKey("SshHostKeyFingerprint"))
				sessionOptions.SshHostKeyFingerprint = config.Value<string>("SshHostKeyFingerprint");


			if (config.ContainsKey("FtpMode"))
				sessionOptions.FtpMode = (FtpMode)Enum.Parse(typeof(FtpMode), config.Value<string>("FtpMode"));

			if (config.ContainsKey("FtpSecure"))
				sessionOptions.FtpSecure = (FtpSecure)Enum.Parse(typeof(FtpSecure), config.Value<string>("FtpSecure"));

			if (config.ContainsKey("GiveUpSecurityAndAcceptAnySshHostKey"))
				sessionOptions.GiveUpSecurityAndAcceptAnySshHostKey = config.Value<bool>("GiveUpSecurityAndAcceptAnySshHostKey");

			if (config.ContainsKey("GiveUpSecurityAndAcceptAnyTlsHostCertificate"))
				sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate = config.Value<bool>("GiveUpSecurityAndAcceptAnyTlsHostCertificate");

			if (config.ContainsKey("Password"))
				sessionOptions.Password = config.Value<string>("Password");

			if (config.ContainsKey("PortNumber"))
				sessionOptions.PortNumber = config.Value<int>("PortNumber");

			if (config.ContainsKey("PrivateKeyPassphrase"))
				sessionOptions.PrivateKeyPassphrase = config.Value<string>("PrivateKeyPassphrase");

			if (config.ContainsKey("TimeoutInMilliseconds"))
				sessionOptions.TimeoutInMilliseconds = config.Value<int>("TimeoutInMilliseconds");
			else if (config.ContainsKey("Timeout"))
				sessionOptions.TimeoutInMilliseconds = config.Value<int>("Timeout");


			if (config.ContainsKey("TlsClientCertificatePath"))
				sessionOptions.TlsClientCertificatePath = config.Value<string>("TlsClientCertificatePath");

			if (config.ContainsKey("TlsHostCertificateFingerprint"))
				sessionOptions.TlsHostCertificateFingerprint = config.Value<string>("TlsHostCertificateFingerprint");

			if (config.ContainsKey("WebdavRoot"))
				sessionOptions.WebdavRoot = config.Value<string>("WebdavRoot");

			if (config.ContainsKey("WebdavSecure"))
				sessionOptions.WebdavSecure = config.Value<bool>("WebdavSecure");

		}

		[STAThread]
		static void Main(string[] args)
		{
			if (Clipboard.ContainsImage())
				Output(UploadImage(Clipboard.GetImage() as Bitmap));
			else if (Clipboard.ContainsFileDropList())
				Output(UploadFile(new FileInfo(Clipboard.GetFileDropList()[0])));
			else if (Clipboard.ContainsText())
			{
				Thread.Sleep(100); // Sometimes there are issue with focus if we do this too fast
				Output(Clipboard.GetText(TextDataFormat.UnicodeText));
			}
			else
			{
				Debug.WriteLine("no clipboard data");
			}
		}

		private static string GetCBText()
		{
			string text = Clipboard.GetText(TextDataFormat.UnicodeText);
			if (string.IsNullOrWhiteSpace(text))
				text = Clipboard.GetText(TextDataFormat.Text);
			if (string.IsNullOrWhiteSpace(text))
				text = Clipboard.GetText(TextDataFormat.Rtf);

			return text;
		}

		private static void Output(string text)
		{
			text = text
				.Replace("{", "{`{")
				.Replace("}", "{}}")
				.Replace("{`{", "{{}")
				.Replace("+", "{+}")
				.Replace("^", "{^}")
				.Replace("%", "{%}")
				.Replace("~", "{~}")
				.Replace("(", "{(}")
				.Replace(")", "{)}")
				.Replace("[", "{[}")
				.Replace("]", "{]}");

			Debug.WriteLine(text);
			SendKeys.SendWait(text);
		}

		private static string UploadFile(FileInfo file)
		{
			try
			{
				LoadConfig("server-config.json");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw;
			}



			IntPtr normalCursor = CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_NORMAL));
			IntPtr iBeamCursor = CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_IBEAM));

			try
			{
				SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_APPSTARTING)), OCR_NORMAL);
				SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_APPSTARTING)), OCR_IBEAM);


				using (Session session = new Session())
				{
					// Connect
					session.Open(sessionOptions);

					// Upload files
					TransferOptions transferOptions = new TransferOptions();
					transferOptions.TransferMode = TransferMode.Binary;

					TransferOperationResult transferResult;
					transferResult =
						session.PutFiles(file.FullName, baseUploadPath + fileDir + file.Name.Replace(" ", "_"), false, transferOptions);

					// Throw on any error
					transferResult.Check();

					return baseUrl + fileDir + file.Name.Replace(" ", "_");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw;
			}
			finally
			{
				SetSystemCursor(normalCursor, OCR_NORMAL);
				SetSystemCursor(iBeamCursor, OCR_IBEAM);

			}

		}

		private static string UploadImage(Bitmap cbImage)
		{
			try
			{
				LoadConfig("server-config.json");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw;
			}



			IntPtr normalCursor = CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_NORMAL));
			IntPtr iBeamCursor = CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_IBEAM));

			try
			{
				SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_APPSTARTING)), OCR_NORMAL);
				SetSystemCursor(CopyIcon(LoadCursor(IntPtr.Zero, (int)OCR_APPSTARTING)), OCR_IBEAM);

				byte[] rawImageData = new byte[Math.Min(2048 * 2048, cbImage.Width * cbImage.Height)];
				BitmapData bmpd = cbImage.LockBits(new Rectangle(0, 0, cbImage.Width, cbImage.Height),
													   ImageLockMode.ReadOnly,
													   PixelFormat.Format32bppArgb);
				Marshal.Copy(bmpd.Scan0, rawImageData, 0, rawImageData.Length);
				cbImage.UnlockBits(bmpd);
				MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
				byte[] hash = md5.ComputeHash(rawImageData);


				string tmpFile = BitConverter.ToString(hash).Replace("-", "").ToLower().PadRight(10, '0').Substring(0, 10) + ".png";

				cbImage.Save(Path.GetTempPath() + tmpFile, System.Drawing.Imaging.ImageFormat.Png);

				using (Session session = new Session())
				{
					// Connect
					session.Open(sessionOptions);

					// Upload files
					TransferOptions transferOptions = new TransferOptions();
					transferOptions.TransferMode = TransferMode.Binary;

					TransferOperationResult transferResult;
					transferResult =
						session.PutFiles(Path.GetTempPath() + tmpFile, baseUploadPath + ssDir + tmpFile, false, transferOptions);

					// Throw on any error
					transferResult.Check();

					File.Delete(Path.GetTempPath() + tmpFile);

					return baseUrl + ssDir + tmpFile;


				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				throw;
			}
			finally
			{
				SetSystemCursor(normalCursor, OCR_NORMAL);
				SetSystemCursor(iBeamCursor, OCR_IBEAM);
			}


		}

		[DllImport("user32.dll")]
		static extern bool SetSystemCursor(IntPtr hcur, uint id);

		[DllImport("user32.dll")]
		static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);


		[DllImport("user32.dll")]
		public static extern IntPtr CopyIcon(IntPtr pcur);

		private const uint OCR_IBEAM = 32513;
		private const uint OCR_NORMAL = 32512;
		private const uint OCR_APPSTARTING = 32650;
	}
}
