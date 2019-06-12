﻿using System.ComponentModel.Composition;
using System.Windows.Controls;
using Caliburn.Micro;
using JetBrains.Annotations;
using LiteDbExplorer.Core;
using LiteDbExplorer.Framework;
using LiteDB;
using LiteDbExplorer.Modules.Shared;
using LiteDbExplorer.Wpf.Framework;
using LiteDbExplorer.Wpf.Framework.Shell;
using MaterialDesignThemes.Wpf;
using Serilog;

namespace LiteDbExplorer.Modules.DbDocument
{

    public class DocumentReferencePayload : IReferenceId
    {
        public DocumentReferencePayload(DocumentReference documentReference)
        {
            InstanceId = documentReference.InstanceId;
            DocumentReference = documentReference;
        }

        public string InstanceId { get; }

        public DocumentReference DocumentReference { get; }
    }

    [Export(typeof(IDocumentPreview))]
    [Export(typeof(DocumentPreviewViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DocumentPreviewViewModel : Document<DocumentReferencePayload>, IDocumentPreview
    {
        private DocumentReference _document;
        private IDocumentDetailView _view;

        [ImportingConstructor]
        public DocumentPreviewViewModel()
        {
            OpenAsDocumentCommand = new RelayCommand(OpenAsDocument, _ => CanOpenAsDocument);

            SplitOrientation = Properties.Settings.Default.DocumentPreview_SplitOrientation;
            ContentMaxLength = Properties.Settings.Default.DocumentPreview_ContentMaxLength;
        }

        public int ContentMaxLength { get; }

        public Orientation? SplitOrientation { get; }

        public override object IconContent => new PackIcon { Kind = PackIconKind.Json, Height = 16 };

        public DocumentReference Document
        {
            get => _document;
            private set
            {
                if (_document != null)
                {
                    _document.ReferenceChanged -= OnDocumentReferenceChanged;
                }
                _document = value;
                if (_document != null)
                {
                    _document.ReferenceChanged += OnDocumentReferenceChanged;
                }

            }
        }
        
        public LiteFileInfo FileInfo { get; private set; }

        public bool IsDocumentView { get; private set; }

        public bool HasDocument => Document != null;

        public bool HasFileInfo => FileInfo != null;

        public bool HideFileInfo => FileInfo == null;

        public bool CanOpenAsDocument => Document != null && !IsDocumentView;

        public RelayCommand OpenAsDocumentCommand { get; set; }
        
        public override void Init(DocumentReferencePayload item)
        {
            Log.Debug("Init. {ViewModelName}, ReferenceId {ReferenceId}", nameof(DocumentPreviewViewModel), item.InstanceId);

            IsDocumentView = true;

            SetActiveDocument(item.DocumentReference);
        }

        public void SetActiveDocument(DocumentReference document)
        {
            Log.Debug("ActiveDocument {ViewModelName}, ReferenceId {ReferenceId}", nameof(DocumentPreviewViewModel), document?.InstanceId);

            InstanceId = document?.InstanceId;

            DisplayName = "Document Preview";

            Document = document;

            if (Document != null)
            {
                DisplayName = Document.ToDisplayName();

                var database = Document.Collection?.Database;
                if (database != null)
                {
                    GroupId = database.InstanceId;
                    GroupDisplayName = database.Name;
                }
            }

            if (document != null && document.Collection is FileCollectionReference reference)
            {
                FileInfo = reference.GetFileObject(document);
            }
            else
            {
                FileInfo = null;
            }
        }

        [UsedImplicitly]
        public void OpenAsDocument(object _)
        {
            IoC.Get<IDocumentSet>().OpenDocument<DocumentPreviewViewModel, DocumentReferencePayload>(new DocumentReferencePayload(Document));
        }

        protected override void OnViewLoaded(object view)
        {
            _view = view as IDocumentDetailView;
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                Log.Debug("Deactivate {ViewModelName}, ReferenceId {ReferenceId}", nameof(DocumentPreviewViewModel), InstanceId);

                FileInfo = null;
                Document = null;
            }
        }
        
        #region Handles
        
        private void OnDocumentReferenceChanged(object sender, ReferenceChangedEventArgs<DocumentReference> e)
        {
            switch (e.Action)
            {
                case ReferenceNodeChangeAction.Remove:
                    TryClose();
                    break;
                case ReferenceNodeChangeAction.Update:
                    SetActiveDocument(e.Reference);
                    _view?.UpdateView(e.Reference);
                    break;
            }
        }

        #endregion
    }
}