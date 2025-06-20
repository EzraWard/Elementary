using Elementary.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elementary.Core.Interfaces
{
    public interface ISettings
    {
        /// <summary>
        /// Current chosen Bible translation.
        /// Default ETranslation.NET.
        /// </summary>
        ETranslation Translation {  get; set; }

        /// <summary>
        /// Current chosen Bible book.
        /// Default Genesis.
        /// </summary>
        EBook Book { get; set; }

        /// <summary>
        /// Current chosen book chapter.
        /// Default 1.
        /// </summary>
        int Chapter { get; set; }

        /// <summary>
        /// Font with which to display Bible text.
        /// Default EFont.SegoeUI.
        /// </summary>
        EFont Font { get; set; }

        /// <summary>
        /// Font size with which to display Bible text.
        /// Default EFontSize.Medium.
        /// </summary>
        EFontSize FontSize { get; set; }

        /// <summary>
        /// Display verse numbers inline.
        /// Default true.
        /// </summary>
        bool? ShowVerseNumbers {  get; set; }

        /// <summary>
        /// The theme of the application.
        /// Default ETheme.System.
        /// </summary>
        ETheme Theme { get; set; }
    }
}
