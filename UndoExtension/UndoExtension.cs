﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using InRule.Authoring.Commanding;
using InRule.Authoring.ComponentModel;
using InRule.Authoring.Media;
using InRule.Authoring.Services;
using InRule.Authoring.Windows;
using InRule.Authoring.Windows.Controls;
using InRule.Repository;
using InRule.Repository.RuleElements;

namespace UndoExtension
{
    public class UndoExtension : ExtensionBase
    {
        private VisualDelegateCommand undoCommand;
        private const int BUFFER_SIZE = 10;

        protected IObservable<RuleRepositoryDefBase> DefActions;
        private IObservable<Unit> UndoClicked => undoSubject.AsObservable();

        private readonly Subject<Unit> undoSubject = new Subject<Unit>();
        protected IObserver<Unit> UndoButtonSubject => undoSubject.AsObserver();

        private readonly CompositeDisposable subscriptionsDisposable = new CompositeDisposable();

        private readonly Stack<UndoHistoryItem> bufferCollection = new Stack<UndoHistoryItem>(BUFFER_SIZE);

        public UndoExtension()
            : base("UndoExtension", "Provides undo functionality on defs", new Guid("{CA41B187-3B1F-48A2-94A2-AAAAF047D453}"))
        {

        }

        public override void Enable()
        {
            undoCommand = new VisualDelegateCommand(Undo, "Undo",
                ImageFactory.GetImageThisAssembly("Images/arrow-undo-16.png"),
                ImageFactory.GetImageThisAssembly("Images/arrow-undo-32.png"));

            var group = IrAuthorShell.HomeTab.GetGroup("Clipboard");
            group.AddButton(undoCommand);

            var defChangedObservable = Observable.FromEventPattern<CancelEventArgs<RuleRepositoryDefBase>>(
                x => RuleApplicationService.Controller.RemovingDef += x,
                x => RuleApplicationService.Controller.RemovingDef -= x);

            DefActions = defChangedObservable.Select(x => x.EventArgs.Item);
            subscriptionsDisposable.Add(
                DefActions.Select(x => new UndoHistoryItem()
                {
                    DefToUndo = x.CopyWithSameGuids(),
                    ParentGuid = x.Parent.Guid,
                    OriginalIndex = ((IContainsRuleElements)x.Parent).RuleElements.IndexOf(x)
                }).Do(LogEvent)
            .Subscribe(x =>
            {
                if (bufferCollection.Count >= BUFFER_SIZE)
                {
                    var p = bufferCollection.Pop();
                    Debug.WriteLine("UNDOSTREAM - Buffer at max capacity. Popped {0} from stack", p.DefToUndo.Name);
                }
                bufferCollection.Push(x);
            }));

            subscriptionsDisposable.Add(
                UndoClicked.Where(x => bufferCollection.Any())
                .Subscribe(x => UndoDefRemoved(bufferCollection.Pop())));
        }

        private void LogEvent(UndoHistoryItem x)
        {
            Debug.WriteLine("UNDOSTREAM - Buffer Count: {3} Name:{0} - DefIndex: {1} Parent: {2}", x.DefToUndo.Name, x.OriginalIndex, x.ParentGuid, bufferCollection.Count);
        }

        private void UndoDefRemoved(UndoHistoryItem item)
        {
            var def = item.DefToUndo;

            Debug.WriteLine("Parent GUID: {0}", item.ParentGuid);
            Debug.WriteLine("undo delete of def: {0}", new object[] { def.Name });

            var ruleApp = RuleApplicationService.RuleApplicationDef;
            if (ruleApp.LookupItem(def.Guid) != null)
            {
                Debug.WriteLine("UNDOSTREAM - DefToUndo {0} already exists in rule application def. Cannot undo a deletion when the target element is already present");
                return;
            }

            var parent = ruleApp.LookupItem(item.ParentGuid) ?? RuleApplicationService.RuleApplicationDef;

            Debug.WriteLine("lookup of parent: {0}", new object[] { parent.Name });

            RuleApplicationService.Controller.InsertDef(def, parent, item.OriginalIndex);
            SelectionManager.SelectedItem = def;

            Debug.WriteLine("Added Def {0} to parent {1} elements collection", def.Name, parent.Name);

        }

        public void Undo(object obj)
        {
            UndoButtonSubject.OnNext(Unit.Default);
        }
    }

    public class UndoHistoryItem
    {
        public Guid ParentGuid { get; set; }
        public int OriginalIndex { get; set; }
        public RuleRepositoryDefBase DefToUndo { get; set; }
    }
}
