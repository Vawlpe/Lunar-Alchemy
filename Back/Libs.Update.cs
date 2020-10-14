using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using MOLE_Back.Properties;

namespace MOLE_Back.Libs
{
	/// <summary>
	/// Verifies and updates libs
	/// </summary>
	public static class Update
	{
		/*
		const int ExpectedAsarVer = 10701;
		const int ExpectedLCVer = 181;

		/// <summary>
		/// Get any Asar build from Appveyor CI
		/// </summary>
		/// <param name="reqstr">Request String used to filter Appveyor build history (if unsure, check PresetAppveyorRequests</param>
		public static void GetAsarBuildAppveyor(string reqstr)
		{
			// Initial Request (Appveyor BNR)
			JObject resp0 = web.WebUtils.GetHttpResponse("https://ci.appveyor.com/api/projects/RPGHacker/asar/history?" + reqstr);
			string BNR = resp0.SelectToken("builds", true)[0].SelectToken("buildNumber", true).Value<String>();
			Console.WriteLine("BNR: " + BNR);

			// Secondary Request (Appveyor JID)
			JObject resp1 = web.WebUtils.GetHttpResponse("https://ci.appveyor.com/api/projects/RPGHacker/asar/build/" + BNR);
			string JID = resp1.SelectToken("build.jobs", true).First.SelectToken("jobId", true).Value<String>();
			Console.WriteLine("JID: " + JID);

			// Download
			web.WebUtils.DownloadFile
			(
				"https://ci.appveyor.com/api/buildjobs/" + JID + "/artifacts/build%2Fasar%2Flibasar.dll",
				"asar.dll - BNR." + BNR + " - JID." + JID + ".dll",
				true
			);

			// Cleanup
			File.Delete(AppDomain.CurrentDomain.BaseDirectory+@"\asar.dll");
			File.Move("asar.dll - BNR." + BNR + " - JID." + JID + ".dll", AppDomain.CurrentDomain.BaseDirectory+@"\asar.dll");
			File.Delete("asar.dll - BNR." + BNR + " - JID." + JID + ".dll");
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);


		}

		/// <summary>
		/// Get the latest build of Asar from the official github
		/// </summary>
		public static void GetAsarLatestRelease()
		{
			//	Extract
			string extractPath = Path.GetFullPath(name).Replace(".zip", String.Empty);
			if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) extractPath += Path.DirectorySeparatorChar;
			Directory.CreateDirectory(extractPath);
			using (ZipArchive archive = ZipFile.OpenRead(name))
			{
				archive.GetEntry(@"dll\asar.dll").ExtractToFile(Path.Combine(extractPath, @"asar.dll"), true);
			}

			// Cleanup
			Directory.SetCurrentDirectory(extractPath);
			File.Delete(name);
			File.Delete(AppDomain.CurrentDomain.BaseDirectory+@"\asar.dll");
			File.Move("asar.dll", AppDomain.CurrentDomain.BaseDirectory+@"\asar.dll");
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			Directory.Delete(extractPath);
		}


		/// <summary>
		/// Collection of presets for Appveyor request strings
		/// </summary>
		public struct PresetAppveyorRequests
		{
			/// <summary>
			/// Latest successful build
			/// </summary>
			public static string LatestSuccess = "recordsNumber=1&branch=master&Status=success";
		}
		*/
		/// <summary>
		/// Make sure all libraries are found, up to date, and compatible
		/// </summary>
		public static void VerifyLibs()
        {
			Console.WriteLine("Verifying Libs");
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			if (!File.Exists("asar.dll")) Console.WriteLine("Asar Not Found\n");
			//if (!File.Exists("lunarcompress.dll")) Console.WriteLine("LunarCompress Not Found"); // do this later
			if (Settings.Default.UPDATE_asar) // autoupdate on
			{
				if (Settings.Default.UPDATE_asar_mode == "release") // releases only
				{
					Dictionary<string, string> rels = GetGHReleases("RPGHacker", "Asar");
					Console.WriteLine("DLURL									:	Prerelease?		Draft?");
					foreach (KeyValuePair<string, string> entry in rels)
					{
						Console.WriteLine("{0}	:	{1}", entry.Key, entry.Value);
					}
				}
				else if (Settings.Default.UPDATE_asar_mode == "build") // latest build
				{
					// Request file
					HttpWebRequest req = (HttpWebRequest)WebRequest.Create(
						new Uri("https://random.muncher.se/ftp/asar/windows/win32/build/asar/MinSizeRel/asar.dll"));

					// Add IfModifiedSince Header
					DateTime targetDate = File.GetLastWriteTime("asar.dll");    // Set a target date to the current files modified date
					req.IfModifiedSince = targetDate;
					try
					{
						// Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
						HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
						using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
						{
							using (StreamWriter sw = new StreamWriter("asar.dll"))
							{
								//sw.Write(sr.ReadToEnd());
								Console.WriteLine("Asar Updated to build last modified at {0} (dl disabled for tests)", resp.LastModified);
							}
						}
					}
					catch (WebException e)
					{
						if (e.Response != null)
						{
							if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
								Console.WriteLine("\nFile has not been modified");
							else
								Console.WriteLine("\nUnexpected status code = " + ((HttpWebResponse)e.Response).StatusCode);
						}
						else
						{
							Console.WriteLine("\nUnexpected Web Exception " + e.Message);
						}
					}
				}
			}
		}

		public static Dictionary<String,String> GetGHReleases(string Owner, string Repo, int Page=1, int Per_Page=100)
		{
			Dictionary<string, string> Releases = new Dictionary<string, string>();

			string URL = String.Format("https://api.github.com/repos/{0}/{1}/releases?page={2},per_page={3}", Owner, Repo, Page, Per_Page);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
			
			req.UserAgent = "Vawlpe/MOLE";
			req.ContentType = "application/json; charset=utf-8";
			req.Accept = "application/vnd.github.v3+json";
			req.AllowAutoRedirect = true;
			
			
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
			JArray parsed = new JArray();
			using (var sr = new StreamReader(resp.GetResponseStream()))
			{
				parsed = JArray.Parse(sr.ReadToEnd());
			}

			for (int i = 0; i < parsed.Count; i++)
			{
				JObject asst = (JObject)parsed[i].SelectToken("assets", true).First;
				Releases.Add(asst.SelectToken("browser_download_url", true).Value<String>(), "p+"+parsed[i].SelectToken("prerelease", true).Value<String>()+"			d+"+ parsed[i].SelectToken("draft", true).Value<String>());
			}
			return Releases;
		}
	}
}