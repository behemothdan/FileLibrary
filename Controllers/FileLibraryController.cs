using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using TitaniumFileLibrary.Models;
using ImageProcessor;
using System.IO.Compression;
using Ionic.Zip;
using TitaniumFileLibrary.Models.ViewModels;
using System.Configuration;
using System.Text.RegularExpressions;

namespace TitaniumFileLibrary.Controllers
{
	[RoutePrefix("FileLibrary")]
	public class FileLibraryController : Controller
	{
		public static string RootPath = ConfigurationManager.AppSettings["LibraryRootPath"];

		string _ActualRootPath = null;
		private string ActualRootPath
		{
			get
			{
				if (_ActualRootPath == null)
				{
					_ActualRootPath = Server.MapPath(RootPath);
				}
				return _ActualRootPath;
			}
		}

		private string CleanFolderPath(string folderPath)
		{
			folderPath = folderPath.Replace("/", "\\");			
			return folderPath;
		}

		/// <summary>
		/// Generate a view of all the files and folders in the file library
		/// </summary>
		/// <param name="FolderPath"></param>
		/// <returns></returns>
		[Route("{*FolderPath}")]	
		public ActionResult Index(string FolderPath)
		{
			if (String.IsNullOrWhiteSpace(FolderPath)) { FolderPath = ""; }
			FolderPath = CleanFolderPath(FolderPath);

			// A combination of our server path and the specific folder we are looking in for files
			string ActualPath = Path.Combine(ActualRootPath, FolderPath);

			//This means that the path that was specified does not exist so I have to take you somewhere, and that would be the root of the file library.
			if (!Directory.Exists(ActualPath)) { return RedirectToAction("Index"); }

			// Add each directory and file to the list so we can work with it
			var libraryItems = new List<FileLibraryItem>();

			// Uses the combination defined above as our working directory
			DirectoryInfo APDI = new DirectoryInfo(ActualPath);

			// List all the directories withing the current folder path
			libraryItems.AddRange(APDI.GetDirectories("*", SearchOption.TopDirectoryOnly).Select(directory => new FileLibraryItem()
			{
				Name = directory.Name,
				Path = directory.FullName.Replace(ActualRootPath, "").Replace("\\", "/"),
				IsDirectory = true
			}));

			// Lists all the individual files within the current folder path
			libraryItems.AddRange(APDI.GetFiles("*", SearchOption.TopDirectoryOnly).Select(file => new FileLibraryItem()
			{
				Name = file.Name,
				Size = file.Length,
				Path = RootPath + CleanFolderPath(FolderPath) + "/" + file.Name,
				FileType = MimeMapping.GetMimeMapping(file.FullName),
				DateAdded = file.LastWriteTime
			}));

			// Generate the breadcrumbs menu at the top of the page
			List<Breadcrumb> breadcrumbNavigation = new List<Breadcrumb>();
			string[] folders = FolderPath.Split('\\');
			for (int i = 0; i < folders.Length; i++)
			{
				var breadcrumbEntry = new Breadcrumb()
				{
					Name = folders[i],
					Path = String.Empty
				};
				for (int r = 0; r <= i; r++)
				{
					if (r != 0)
					{
						breadcrumbEntry.Path += "/";
					}
					breadcrumbEntry.Path += folders[r];
				}
				if (!String.IsNullOrWhiteSpace(breadcrumbEntry.Name))
				{
					breadcrumbNavigation.Add(breadcrumbEntry);
				}
			}

			ViewBag.FolderPath = FolderPath;
			ViewBag.FolderMenu = breadcrumbNavigation;
			if (TempData["LibraryPostbackMessage"] != null)
			{
				ViewBag.LibraryPostbackMessage = TempData["LibraryPostbackMessage"];
			}
			return View(libraryItems);
		}

		/// <summary>
		/// Recursively search all the files and folders
		/// </summary>
		/// <param name="FolderPath"></param>
		/// <param name="SearchQuery"></param>
		/// <returns></returns>
		[Route("Search/{*FolderPath}")]
		// Generate the view of all the files currently in the library
		public ActionResult Search(string FolderPath, string SearchQuery = "*")
		{
			if (String.IsNullOrWhiteSpace(FolderPath)) { FolderPath = ""; }
			FolderPath = CleanFolderPath(FolderPath);

			// A combination of our server path and the specific folder we are looking in for files
			string ActualPath = Path.Combine(ActualRootPath, FolderPath);

			//This means that the path that was specified does not exist so I have to take you somewhere, and that would be the root of the file library.
			if (!Directory.Exists(ActualPath)) { return RedirectToAction("Index"); }

			// Add each directory and file to the list so we can work with it
			var libraryItems = new List<FileLibraryItem>();

			// Uses the combination defined above as our working directory
			DirectoryInfo APDI = new DirectoryInfo(ActualPath);

			// Drop the search query to lower case since we do the same thing to file and folder names
			SearchQuery = SearchQuery.ToLower();

			libraryItems.AddRange(APDI.GetDirectories("*", SearchOption.AllDirectories)
				.Where(d =>
					d.CreationTime.ToString().ToLower().Contains(SearchQuery)
					|| String.Format("{0:f}", d.CreationTime).ToString().ToLower().Contains(SearchQuery)
					|| d.Name.ToLower().Contains(SearchQuery)
				)
				.Select(directory => new FileLibraryItem()
			{
				Name = directory.Name,
				Path = directory.FullName.Replace(ActualRootPath, "").Replace("\\", "/"),
				IsDirectory = true,
				DateAdded = directory.CreationTime
			}));

			libraryItems.AddRange(APDI.GetFiles("*", SearchOption.AllDirectories)
				.Where(f =>
					f.CreationTime.ToString().ToLower().Contains(SearchQuery)
					|| String.Format("{0:f}", f.CreationTime).ToString().ToLower().Contains(SearchQuery)
					|| MimeMapping.GetMimeMapping(f.Name).ToString().ToLower().Contains(SearchQuery)
					|| f.Length.ToString().ToLower().Contains(SearchQuery)
					|| f.Name.ToLower().Contains(SearchQuery)
				)
				.Select(file => new FileLibraryItem()
			{
				Name = file.Name,
				Size = file.Length,
				Path = RootPath + FolderPath.Replace("\\", "/") + "/" + file.Name,
				FileType = MimeMapping.GetMimeMapping(file.FullName),
				DateAdded = file.LastWriteTime
			}));

			// Generate the breadcrumbs menu at the top of the page
			List<Breadcrumb> breadcrumbNavigation = new List<Breadcrumb>();
			string[] folders = FolderPath.Split('\\');
			for (int i = 0; i < folders.Length; i++)
			{
				var breadcrumbEntry = new Breadcrumb()
				{
					Name = folders[i],
					Path = String.Empty
				};
				for (int r = 0; r <= i; r++)
				{
					if (r != 0)
					{
						breadcrumbEntry.Path += "/";
					}
					breadcrumbEntry.Path += folders[r];
				}
				if (!String.IsNullOrWhiteSpace(breadcrumbEntry.Name))
				{
					breadcrumbNavigation.Add(breadcrumbEntry);
				}
			}

			ViewBag.FolderPath = FolderPath;
			ViewBag.FolderMenu = breadcrumbNavigation;
			if (TempData["LibraryPostbackMessage"] != null)
			{
				ViewBag.LibraryPostbackMessage = TempData["LibraryPostbackMessage"];
			}
			return PartialView(libraryItems);
		}

		/// <summary>
		/// Save the cropped image
		/// </summary>
		/// <param name="imageData"></param>
		/// <param name="originalImageName"></param>
		/// <param name="FolderPath"></param>
		/// <returns></returns>
		[HttpPost, Route("SaveCrop/{*FolderPath}")]
		public ActionResult SaveImage(string imageData, string originalImageName, string FolderPath)
		{
			string currentDate = DateTime.Now.TimeOfDay.ToString();
			currentDate = Regex.Replace(currentDate, @"[/,\,:,;]", "");
			string originalImageNameNoExtension = Path.GetFileNameWithoutExtension(originalImageName);
			string newImageName = originalImageNameNoExtension + "-" + currentDate + "-cropped" + Path.GetExtension(originalImageName);
			newImageName = newImageName.Replace(" ", "");

			// Full path for saving the image to the server
			string newFilePath = ActualRootPath;

			// Relative path for updating the image within the modal window
			string returnedImageURL = "/Content/Uploads/";
			if (!String.IsNullOrWhiteSpace(FolderPath))
			{
				newFilePath += CleanFolderPath(FolderPath) + "\\";
				returnedImageURL += CleanFolderPath(FolderPath) + "\\";
			}
			newFilePath += newImageName;
			returnedImageURL += newImageName;

			using (FileStream stream = new FileStream(newFilePath, FileMode.Create))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					byte[] data = Convert.FromBase64String(imageData);
					writer.Write(data);
					writer.Close();
				}
				stream.Close();
			}
			return Json(new { FolderPath = returnedImageURL });
		}
		
		/// <summary>
		/// Save the modified image using the query string variables
		/// </summary>
		/// <param name="imageURL"></param>
		/// <param name="originalImageName"></param>
		/// <param name="FolderPath"></param>
		/// <returns></returns>
		[HttpPost, Route("Save/{*FolderPath}")]		
		public ActionResult Save(string imageURL, string originalImageName, string FolderPath)
		{
			WebClient newImageClient = new WebClient();
			
			string currentDate = DateTime.Now.TimeOfDay.ToString();

			// Strip out weird characters from the date and time
			currentDate = Regex.Replace(currentDate, @"[/,\,:,;]", "");

			// Get the original image name without the extension so we can append the unique identifier
			string originalImageNameNoExtension = Path.GetFileNameWithoutExtension(originalImageName);

			// Attach the time and edit flag and reattach the file extension to use as a new file name
			string newImageName = originalImageNameNoExtension + "-" + currentDate + "-edit" + Path.GetExtension(originalImageName);

			// Make sure there are no spaces in the new file name
			newImageName = newImageName.Replace(" ", "");

			// Full path for saving the image to the server
			string newFilePath = ActualRootPath;
			string newImageToDisplay = "/Content/Uploads/";


			if (!String.IsNullOrWhiteSpace(FolderPath))
			{
				newFilePath += CleanFolderPath(FolderPath) + "\\";
				newImageToDisplay += CleanFolderPath(FolderPath) + "\\";
			}

			newFilePath += newImageName;
			newImageToDisplay += newImageName;

			newImageClient.DownloadFile(String.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, HttpRuntime.AppDomainAppVirtualPath).TrimEnd('/') + imageURL, newFilePath);

			return Json(new { FolderPath = newImageToDisplay });			
		}

		/// <summary>
		/// Upload files to the file library
		/// </summary>
		/// <param name="FolderPath"></param>
		/// <param name="fileupload"></param>
		/// <returns></returns>
		[HttpPost, Route("Upload/{*FolderPath}")]
		public ActionResult Upload(string FolderPath, List<HttpPostedFileBase> fileupload)
		{
			foreach (string fileName in Request.Files)
			{
				HttpPostedFileBase file = Request.Files[fileName];
				if (TempData["LibraryPostbackMessage"] != null) { break; } // Don't continue if there is already an error on a prior file
				try
				{
					string newfilePath = ActualRootPath;
					if (!String.IsNullOrWhiteSpace(FolderPath))
					{
						newfilePath += CleanFolderPath(FolderPath) + "\\";
					}
					newfilePath += file.FileName;

					// Verify the file doesn't already exist in the current folder
					if (!System.IO.File.Exists(newfilePath))
					{
						file.SaveAs(newfilePath);
					}
					else
					{
						TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
						{
							Message = "One or more files you attempted to upload already exists in this folder. Please rename the files or try uploading to a new folder."
						};
					}
				}
				catch
				{
					TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
					{
						Message = "The file upload was unsuccessful. Please try again."
					};
				}
			}

			if (TempData["LibraryPostbackMessage"] == null) //If I left this in the foreach then I might override an earlier message.
			{
				TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
				{
					Type = LibraryPostbackMessage.MessageType.success,
					Message = "The file upload was successful."
				};
			}
			return RedirectToAction("Index", "FileLibrary", new { folderPath = FolderPath });
		}

		/// <summary>
		/// Create new directories
		/// </summary>
		/// <param name="FolderPath"></param>
		/// <param name="directoryName"></param>
		/// <returns></returns>
		[HttpPost, Route("create/{*FolderPath}")]
		public ActionResult Create(string FolderPath, string directoryName)
		{
			directoryName = CleanFolderPath(directoryName.Trim());

			if (String.IsNullOrWhiteSpace(directoryName))
			{
				TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
				{
					Type = LibraryPostbackMessage.MessageType.warning,
					Message = "The folder name cannot be blank. Please enter a folder name."
				};
			}
			else
			{
				string newFolderPath = ActualRootPath;
				if (!String.IsNullOrWhiteSpace(FolderPath))
				{
					newFolderPath += CleanFolderPath(FolderPath) + "\\";
				}
				newFolderPath += directoryName;

				if (Directory.Exists(newFolderPath))
				{
					TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
					{
						Message = "There is already a folder with that name."
					};
				}
				else
				{
					try
					{
						Directory.CreateDirectory(newFolderPath);
					}
					catch
					{
						TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
						{
							Message = "Unable to create the specified folder."
						};
					}
				}
			}
			return RedirectToAction("Index", "FileLibrary", new { FolderPath = FolderPath });
		}

		/// <summary>
		/// Delete files and folders from the file library
		/// </summary>
		/// <param name="DeletePath"></param>
		/// <returns></returns>
		[HttpPost, Route("delete/{*DeletePath}")]
		public ActionResult Delete(string DeletePath)
		{
			string ParentFolder = String.Empty;
			if (String.IsNullOrWhiteSpace(DeletePath))
			{
				TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
				{
					Message = "Please select an item to delete."
				};
			}
			else
			{
				string FolderPath = DeletePath.Replace(RootPath, "").Replace("/", "\\");
				if (FolderPath.Length > 0 && FolderPath.StartsWith("\\"))
				{
					FolderPath = FolderPath.Substring(1, FolderPath.Length - 1);
				}
				FolderPath = ActualRootPath + FolderPath;
				if (Directory.Exists(FolderPath) || System.IO.File.Exists(FolderPath))
				{
					ParentFolder = Path.GetDirectoryName(FolderPath) + "\\";
					FileAttributes attr = System.IO.File.GetAttributes(FolderPath);
					
					if (attr.HasFlag(FileAttributes.Directory)) //Check to see if the delete path is a folder
					{
						try
						{
							Directory.Delete(FolderPath, true);
							TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
							{
								Type = LibraryPostbackMessage.MessageType.success,
								Message = "The folder and it's contents was removed successfully."
							};
						}
						catch
						{
							TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
							{
								Message = "The folder was unable to be removed."
							};
						}
					}
					else //The delete path is a file
					{
						try
						{
							System.IO.File.Delete(FolderPath);
							TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
							{
								Type = LibraryPostbackMessage.MessageType.success,
								Message = "The file was removed successfully."
							};
						}
						catch
						{
							TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
							{
								Message = "The file was unable to be removed."
							};
						}
					}
				}
				else
				{
					TempData["LibraryPostbackMessage"] = new LibraryPostbackMessage()
					{
						Message = "This file does not exist or has already been deleted."
					};
				}
			}
			return RedirectToAction("Index", "FileLibrary", new { FolderPath = ParentFolder.Replace(ActualRootPath, String.Empty) });
		}

		/// <summary>
		/// Create a ZIP file of the contents of a folder path and download it to the user's machine
		/// Will not store a copy of the ZIP on the server
		/// </summary>
		/// <param name="FolderPath"></param>
		/// <param name="folderToDownloadVar"></param>
		/// <returns></returns>
		[HttpPost, Route("ZipFolder/{*FolderPath}")]
		public ActionResult ZipFolder(string FolderPath, string folderToDownloadVar)
		{
			string pathToFolder = ActualRootPath;
			if (!String.IsNullOrWhiteSpace(FolderPath))
			{
				pathToFolder += CleanFolderPath(FolderPath) + "\\";
			}
			pathToFolder += folderToDownloadVar;

			Response.Clear();
			Response.BufferOutput = false;
			string archiveName = folderToDownloadVar + "_archive.zip";
			Response.ContentType = "application.zip";
			Response.AddHeader("content-disposition", "filename=" + archiveName);

			using (ZipFile folderArchive = new ZipFile())
			{
				folderArchive.AddDirectory(pathToFolder, folderToDownloadVar);
				folderArchive.Save(Response.OutputStream);
			}
			return RedirectToAction("Index", "FileLibrary", new { FolderPath = FolderPath });
		}
	}
}