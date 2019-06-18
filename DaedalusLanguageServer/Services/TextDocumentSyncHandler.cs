using DaedalusLanguageServer;
using DaedalusLib.Parser;
using OmniSharp.Extensions.Embedded.MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DemoLanguageServer.Services
{
    public class TextDocumentSyncHandler : ITextDocumentSyncHandler
    {
        private readonly ILanguageServer _router;
        private readonly BufferManager _bufferManager;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter() { Pattern = "**/*.d" },
            new DocumentFilter() { Pattern = "**/*.D" }
        );

        private SynchronizationCapability _capability;

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentPath = request.TextDocument.Uri.ToString();
            var text = request.ContentChanges.FirstOrDefault()?.Text;
            _bufferManager.UpdateBuffer(documentPath, text.ToCharArray());

            _router.Window.LogInfo($"Updated buffer for document: {documentPath} ({text.Length} chars)");

            return Unit.Task;
        }
        public TextDocumentSyncHandler(ILanguageServer router, BufferManager bufferManager)
        {
            this._router = router;
            this._bufferManager = bufferManager;
        }
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                SyncKind = Change
            };
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentPath = request.TextDocument.Uri.ToString();
            var text = request.TextDocument.Text;
            _bufferManager.UpdateBuffer(documentPath, text.ToCharArray());

            Parse(request.TextDocument.Uri, cancellationToken);
            return Unit.Task;
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
            };
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public void SetCapability(SynchronizationCapability capability)
        {
            _capability = capability;
        }

        public void Parse(Uri uri, CancellationToken cancellation)
        {
            var workspaces = _router.Workspace.WorkspaceFolders().GetAwaiter().GetResult();
            var path = uri.LocalPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && path.StartsWith("/"))
            {
                path = Path.GetFullPath(path.Substring(1));
            }
            _router.Window.LogInfo(path);
            var parserResult = DaedalusParserHelper.Load(path);
            if (parserResult.ErrorMessages.Count > 0)
            {
                _router.Document.PublishDiagnostics(new PublishDiagnosticsParams
                {
                    Uri = uri,
                    Diagnostics = new Container<Diagnostic>(parserResult.ErrorMessages
                        .Select(x => new Diagnostic
                        {
                            Message = x.Message,
                            Range = new Range(new Position(x.Line - 1, x.Column), new Position(x.Line - 1, x.Column)),
                        }))
                });
            }
            else
            {
                // Clear diagnostics by sending emtpy container.
                _router.Document.PublishDiagnostics(new PublishDiagnosticsParams { Uri = uri, Diagnostics = new Container<Diagnostic>() });
            }
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            Parse(request.TextDocument.Uri, cancellationToken);
            return Unit.Task;
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                IncludeText = false,
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new TextDocumentAttributes(uri, "daedalus");
        }
    }
}
