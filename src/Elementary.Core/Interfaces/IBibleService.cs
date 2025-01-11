using Elementary.Core.Enums;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elementary.Core.Interfaces
{
    public interface IBibleService
    {
        Bible GetBible(ETranslation translation);
    }
}
