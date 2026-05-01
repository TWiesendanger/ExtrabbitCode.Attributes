using System;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

internal static class TransactionHelper
{
    internal static void Run(Document document, string name, Action operation)
    {
        Transaction tx = Globals.InvApp.TransactionManager.StartTransaction((_Document)document, name);
        try
        {
            operation();
            tx.End();
        }
        catch
        {
            tx.Abort();
            throw;
        }
    }
}
