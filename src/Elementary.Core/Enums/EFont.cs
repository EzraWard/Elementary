using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Elementary.Core.Enums
{
    public enum EFont
    {
        [Display(Name = "NotSet")]
        NotSet,

        [Display(Name = "Segoe UI")]
        SegoeUIVariable,

        [Display(Name = "Georgia")]
        Georgia
    }
}
