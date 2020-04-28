using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProxyServer.HTTP
{
  public class  ItemBase
  {
    private string _Source = String.Empty;
    public string Source
    {
      get
      {
        return _Source;
      }
    }
    public ItemBase(string source)
    {
      _Source = source;
    }

  }
}
