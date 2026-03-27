using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KeePass.Tests
{
    /// <summary>
    /// Inicialización global del ensamblado de tests.
    /// Limpia los Debug.Listeners para evitar que los Debug.Assert(false)
    /// del código de producción abran diálogos y bloqueen el proceso de tests.
    /// </summary>
    [TestClass]
    public class TestAssemblySetup
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            // Sin este limpiado, Debug.Assert(false) en código de producción
            // abre un MessageBox que bloquea al proceso de pruebas en un entorno sin UI.
            Debug.Listeners.Clear();
        }
    }
}
