using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TitaniumFileLibrary.Models.ViewModels
{
	public class FileLibraryItem
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public long Size { get; set; }
		public string FileType { get; set; }
		public DateTime DateAdded { get; set; }
		public bool IsDirectory { get; set; }
	}

	public static class FileLibraryReferences
	{
		public static string[] ImageMimeTypes = new string[]
		{
			"image/jpeg",
			"image/png",
			"image/bmp",
			"image/gif",
			"image/svg+xml",
			"image/tiff",
			"image/webp"
		};

		public static string[] DocumentMimeTypes = new string[]
		{
			"application/pdf",
			"application/msword",
			"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			"text/plain",
			"application/vnd.ms-excel",
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			"application/xml",
			"audio/mpeg3"
		};
	}
}