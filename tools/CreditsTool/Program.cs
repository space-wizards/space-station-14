using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace CreditsTool
{
	class Program
	{
		const string ConfigPath = "remappings.txt";
		const string OutputLocation = "./credit_pngs";
		const int world_icon_size = 32;
		//this downloads all the user images of contributors of a passed github repository
		//usage ./CreditsTool <repo owner> <repo> [authToken (helps with github rate limiter)]
		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: ./CreditsTool.exe <repo owner> <repo> [authToken]");
				return;
			}

			if (Directory.Exists(OutputLocation))
			{
				Console.WriteLine(String.Format("Aborted: {0} exists!", OutputLocation));
				return;
			}
			Directory.CreateDirectory(OutputLocation);

			string repoOwner = args[0],
				repoName = args[1],
				authToken = args.Length > 2 ? args[2] : null;

			Console.WriteLine("Querying contributors API...");

			var FirstResponse = GetPageResponse(repoOwner, repoName, authToken, 1);

			Console.WriteLine("Collecting avatar URLs...");

			var LoginAvatars = new Dictionary<string, string>();
			//now list the things we want: avatar urls and logins
			foreach (var I in LoadPages(FirstResponse, repoOwner, repoName, authToken)) {
				var avurl = (string)I["avatar_url"];
				LoginAvatars.Add((string)I["login"], String.Format("{0}{1}s={2}", avurl, avurl.Contains("?") ? "&" : "?", world_icon_size));
			}

			Console.WriteLine(String.Format("Collected info for {0} contributors.", LoginAvatars.Count));

			Console.WriteLine("Remapping github logins...");

			var remaps = LoadConfig();

			Console.WriteLine(String.Format("Downloading and converting avatars to {0} (this will take a while)...", OutputLocation));

			using (var client = new WebClient())
			{
				var count = 0;
				foreach (var I in LoginAvatars)
				{
					var writtenFilename = I.Key;
					if (remaps.TryGetValue(writtenFilename, out string tmp))
					{
						if (tmp == "__REMOVE__")
							continue;
						writtenFilename = tmp;
					}
					using (var stream = new MemoryStream(client.DownloadData(I.Value)))
					using (var originalBMP = new Bitmap(stream))
					{
						if (originalBMP.Width == world_icon_size && originalBMP.Height == world_icon_size)  //no need to resize
							SaveBMP(originalBMP, writtenFilename);
						else
							using (var resizedBMP = new Bitmap(originalBMP, new Size(world_icon_size, world_icon_size)))
								SaveBMP(resizedBMP, writtenFilename);
					}
					Console.WriteLine(String.Format("Done {0}.png! {1}%", writtenFilename, (int)((((float)(count + 1)) / LoginAvatars.Count) * 100)));
					++count;
				}
			}
		}

		static void SaveBMP(Bitmap bmp, string writtenFilename)
		{
			bmp.Save(String.Format("{0}{1}{2}.png", OutputLocation, Path.DirectorySeparatorChar, writtenFilename), ImageFormat.Png);
		}

		static IDictionary<string, string> LoadConfig()
		{
			var result = new Dictionary<string, string>();
			if (File.Exists(ConfigPath))
				foreach (var I in File.ReadAllLines(ConfigPath))
					if (!String.IsNullOrWhiteSpace(I) && I[0] != '#')
					{
						var splits = new List<string>(I.Split(' '));
						if (splits.Count >= 1 && !String.IsNullOrEmpty(splits[1]))
						{
							var key = splits[0];
							splits.RemoveAt(0);
							result.Add(key, String.Join(" ", splits));
						}
					}
			return result;
		}

		static IEnumerable<IDictionary<string, object>> LoadPages(WebResponse firstResponse, string repoOwner, string repoName, string authToken)
		{
			int numPages = GetNumPagesOfContributors(firstResponse);
			Console.WriteLine(String.Format("Downloading {0} pages of contributor info...", numPages));
			//load and combine json for all pages
			var jss = new JavaScriptSerializer();
			using (var sr = new StreamReader(firstResponse.GetResponseStream()))
				foreach (var J in jss.Deserialize<IEnumerable<IDictionary<string, object>>>(sr.ReadToEnd()))
					yield return J;

			//skip the first
			for (var I = 2; I <= numPages; ++I)
				using (var sr = new StreamReader(GetPageResponse(repoOwner, repoName, authToken, I).GetResponseStream()))
					foreach (var J in jss.Deserialize<IEnumerable<IDictionary<string, object>>>(sr.ReadToEnd()))
						yield return J;
		}

		static int GetNumPagesOfContributors(WebResponse response)
		{
			var splits = response.Headers["Link"].Split(',');
			foreach (var I in splits)
				if (I.Contains("rel=\"last\"")) //our boy
				{
					var pagestrIndex = I.IndexOf("&page=") + 6;
					var closingIndex = I.IndexOf('>', pagestrIndex + 1);
					var thedroidswerelookingfor = I.Substring(pagestrIndex, closingIndex - pagestrIndex);
					return Convert.ToInt32(thedroidswerelookingfor);
				}
			return 1;
		}

		static WebResponse GetPageResponse(string repoOwner, string repoName, string authToken, int pageNumber)
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(String.Format("https://api.github.com/repos/{0}/{1}/contributors?per_page=100&page={2}", repoOwner, repoName, pageNumber));
			httpWebRequest.Method = WebRequestMethods.Http.Get;
			httpWebRequest.Accept = "application/json";
			httpWebRequest.UserAgent = "tgstation-13-credits-tool";
			if (authToken != null)
				httpWebRequest.Headers.Add(String.Format("Authorization: token {0}", authToken));
			return httpWebRequest.GetResponse();
		}
	}
}
