using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TitaniumFileLibrary.Models.ViewModels
{
	public class LibraryPostbackMessage
	{
		public enum MessageType
		{
			danger,
			info,
			warning,
			success
		}

		/// <summary>
		/// Default Type is danger.
		/// </summary>
		public MessageType Type { get; set; }
		public string Message { get; set; }

		public LibraryPostbackMessage()
		{
			Type = MessageType.danger;
		}
	}
}