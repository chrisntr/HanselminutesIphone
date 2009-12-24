
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Hanselminutes
{


	public class Podcast
	{

		public string Url {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
		
		public Podcast ()
		{
			
		}
	}
	
	public static class PodcastHelper
	{
	
		public static List<Podcast> ParsePodcastXml(string xml)
		{
			XDocument xDoc = XDocument.Parse(xml);
			XNamespace ns = "";
			//XNamespace ns = "http://www.w3.org/2005/Atom";
			Console.WriteLine ("Entry descendants: " + xDoc.Descendants(ns + "item").Count());
			var podcasts = from x in xDoc.Descendants(ns + "item")
			select new Podcast
			{
				Url = x.Descendants( ns + "enclosure").Attributes("url").First().Value,
				Title = x.Descendants( ns + "title").First().Value
			};
			return podcasts.ToList();
		}
	}
}
