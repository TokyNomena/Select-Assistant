using Autodesk.Revit.UI;

namespace SelectionSet.Adapters
{
    internal interface IExternalEventWrapper : IDisposable
    {
        void Raise();
    }

    internal interface IExternalEventFactory
    {
        IExternalEventWrapper Create(IExternalEventHandler handler);
    }

    internal class RevitExternalEventWrapper : IExternalEventWrapper
    {
        private readonly ExternalEvent externalEvent;

        public RevitExternalEventWrapper(IExternalEventHandler handler)
        {
            externalEvent = ExternalEvent.Create(handler);
        }

        public void Raise()
        {
            externalEvent.Raise();
        }

        public void Dispose()
        {
            externalEvent.Dispose();
        }
    }

    internal class RevitExternalEventFactory : IExternalEventFactory
    {
        public IExternalEventWrapper Create(IExternalEventHandler handler)
        {
            return new RevitExternalEventWrapper(handler);
        }
    }
}
