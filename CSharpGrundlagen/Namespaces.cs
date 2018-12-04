namespace NamespaceA
{
    namespace NamespaceB
    {
        namespace NamespaceC
        {
            public class Info
            {
                public static void ShowInfo()
                {
                    System.Console.WriteLine("ABC");
                }
            }
        }
    }
}

namespace CSharpGrundlagen_1
{
    public class ABC
    {
        public void Show()
        {
            NamespaceA.NamespaceB.NamespaceC.Info.ShowInfo();
        }
    }
}

namespace CSharpGrundlagen_2
{
    using NamespaceA.NamespaceB.NamespaceC;

    public class ABC
    {
        public void Show()
        {
            Info.ShowInfo();
        }
    }
}
