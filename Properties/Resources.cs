using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace hadesFirm.Properties
{
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
  [DebuggerNonUserCode]
  [CompilerGenerated]
  internal class Resources
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    internal Resources()
    {
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) hadesFirm.Properties.Resources.resourceMan, (object) null))
          hadesFirm.Properties.Resources.resourceMan = new ResourceManager("hadesFirm.Properties.Resources", typeof (hadesFirm.Properties.Resources).Assembly);
        return hadesFirm.Properties.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return hadesFirm.Properties.Resources.resourceCulture;
      }
      set
      {
        hadesFirm.Properties.Resources.resourceCulture = value;
      }
    }
  }
}
