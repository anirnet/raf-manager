﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using System.Windows.Forms;

using System.Drawing;

using ItzWarty;

using RAFLib;

using System.IO;

namespace RAFManager
{
    partial class MainForm : Form
    {
        //Names of columns - For finding cells in rows
        private const string CN_USE = "shouldUseMod";
        private const string CN_LOCALPATH = "localPathColumn";
        private const string CN_LOCALPATHPICKER = "pickLocalPathColumn";
        private const string CN_RAFPATH = "rafPathColumn";
        private const string CN_RAFPATHPICKER = "pickRafPathColumn";

        /// <summary>
        /// Initializes the changes view - Sizes columns and sets an event handler for them to autosize
        /// </summary>
        private void InitializeChangesView()
        {
            rafContentView.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(rafContentView_NodeMouseDoubleClick);
            rafContentView.AllowDrop = true;
            rafContentView.DragOver += new DragEventHandler(rafContentView_DragOver);
            rafContentView.MouseClick += new MouseEventHandler(rafContentView_MouseClick);

            changesView = new TristateTreeView();
            changesView.Dock = DockStyle.Fill;
            changesView.EmptyComment = "No items have been added yet!  Drag skin files in!\nClick Help->Guide to open up the manual!";
            changesView.AllowDrop = true;
            changesView.DragOver += new DragEventHandler(changesView_DragOver);
            changesView.DragDrop += new DragEventHandler(changesView_DragDrop);
            changesView.NodeRightClicked += new NodeRightClickedHandler(changesView_NodeRightClicked);
            this.smallContainer.Panel2.Controls.Add(changesView);
            //changesView.Nodes[0].
            //this.Resize += delegate(object sender, EventArgs e) { UpdateChangesGUI(); };
        }

        void rafContentView_MouseClick(object sender, MouseEventArgs e)
        {
            rafContentView.SelectedNode = rafContentView.GetNodeAt(e.Location);
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                RAFInMemoryFileSystemObject fso = (RAFInMemoryFileSystemObject)rafContentView.SelectedNode;
                if(fso != null)
                {
                    ContextMenu cm = new ContextMenu();
                    MenuItem dump = new MenuItem("Dump");
                    dump.Click += delegate(Object sender2, EventArgs e2)
                    {
                        if (fso.GetFSOType() == RAFFSOType.ARCHIVE || fso.GetFSOType() == RAFFSOType.DIRECTORY)
                        {
                            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                            folderDialog.Description = "Select the folder where you wish to dump the files";
                            folderDialog.ShowDialog();
                            if (folderDialog.SelectedPath != "")
                            {
                                Title("Begin Dumping: " + fso.GetRAFPath());
                                DumpRafArchiveByFSO(fso, folderDialog.SelectedPath);
                                Log("Done dumping: " + fso.GetRAFPath());
                                Title(GetWindowTitle());
                            }
                        }
                        else
                        {
                            SaveFileDialog sfd = new SaveFileDialog();
                            string fileName = fso.GetRAFPath().Replace("\\", "/").Split("/").Last();
                            string fileExt = fileName.Substring(fileName.LastIndexOf(".") + 1);
                            sfd.Filter = "File|." + fileExt;
                            sfd.FileName = fileName;
                            sfd.ShowDialog();

                            if (sfd.FileName != "")
                            {
                                Log("Begin dumping: "+fileName);
                                File.WriteAllBytes(
                                    sfd.FileName, 
                                    ResolveRAFPathToEntry(
                                        fso.GetTopmostParent().Text + "/" + fso.GetRAFPath()
                                    ).GetContent()
                                );
                                Log("Done dumping: "+fileName);
                            }
                        }
                    };
                    cm.MenuItems.Add(dump);
                    if(fso.GetFSOType() == RAFFSOType.FILE)
                    {
                        MenuItem viewAsTextFile = new MenuItem("View As Text File");
                        viewAsTextFile.Click += delegate(Object sender2, EventArgs e2)
                        {
                            new TextViewer(
                                ResolveRAFPathToEntry(
                                    fso.GetTopmostParent().Text + "/" + fso.GetRAFPath()
                                )
                            ).Show();
                        };
                        cm.MenuItems.Add(viewAsTextFile);

                        MenuItem viewAsBinary = new MenuItem("View As Binary File");
                        viewAsBinary.Click += delegate(Object sender2, EventArgs e2)
                        {
                            new BinaryViewer(this.baseTitle + " - " + fso.GetTopmostParent()+"/"+fso.GetRAFPath(),
                                ResolveRAFPathToEntry(
                                    fso.GetTopmostParent().Text + "/" + fso.GetRAFPath()
                                ).GetContent()
                            ).Show();
                        };
                        cm.MenuItems.Add(viewAsBinary);

                        if (fso.GetRAFPath().ToLower().EndsWith("inibin") || fso.GetRAFPath().ToLower().EndsWith("troybin"))
                        {
                            MenuItem viewAsINIBIN = new MenuItem("View As INIBIN File");
                            viewAsINIBIN.Click += delegate(Object sender2, EventArgs e2)
                            {
                                try
                                {
                                    new TextViewer(this.baseTitle + " - inibin/troybin view - " + fso.GetTopmostParent() + "/" + fso.GetRAFPath(),
                                        new InibinFile().main(
                                            ResolveRAFPathToEntry(
                                                fso.GetTopmostParent().Text + "/" + fso.GetRAFPath()
                                            ).GetContent()
                                        )
                                    ).Show();
                                    return;
                                }
                                catch { Log("Error parsing INIBIN/TROYBIN file"); }
                            };
                            cm.MenuItems.Add(viewAsINIBIN);
                        }
                    }
                    cm.Show(rafContentView, new Point(e.X, e.Y));
                }
            }
        }
        void DumpRafArchiveByFSO(RAFInMemoryFileSystemObject fso, string dumpDirectory)
        {
            Title("Dump: " + fso.GetRAFPath());
            if (fso.GetFSOType() == RAFFSOType.ARCHIVE || fso.GetFSOType() == RAFFSOType.DIRECTORY)
            {
                PrepareDirectory(dumpDirectory + "/" + fso.GetRAFPath());
            }
            else
            {
                string containingFolderPath = fso.GetRAFPath().Replace("\\", "/").Reverse();
                containingFolderPath = containingFolderPath.Substring(Math.Max(containingFolderPath.IndexOf("/"), 0));
                containingFolderPath = containingFolderPath.Reverse();
                PrepareDirectory(dumpDirectory + "/" + fso.GetRAFPath());
                Console.WriteLine("Dump to: " + dumpDirectory + "/" + fso.GetRAFPath());
                File.WriteAllBytes(dumpDirectory + "/" + fso.GetRAFPath(),
                    ResolveRAFPathToEntry(fso.GetTopmostParent().Text + "/" + fso.GetRAFPath()).GetContent()
                );
            }
            for (int i = 0; i < fso.Nodes.Count; i++)
                DumpRafArchiveByFSO((RAFInMemoryFileSystemObject)fso.Nodes[i], dumpDirectory);
        }

        void changesView_NodeRightClicked(TristateTreeNode node, MouseEventArgs e)
        {
            ContextMenu cm = new ContextMenu();
            MenuItem delete = new MenuItem("Delete");
            delete.Click += delegate(object sender, EventArgs e2){
                if (node.Parent is TristateTreeView)
                {
                    TristateTreeView p = (TristateTreeView)node.Parent;
                    p.Nodes.Remove(node);
                    if (p.SelectedNode == node) p.SelectedNode = null;
                    p.Invalidate();
                }
                else
                {
                    TristateTreeNode n = (TristateTreeNode)node.Parent;
                    n.Nodes.Remove(node);
                    if (n.TreeView.SelectedNode == node) n.TreeView.SelectedNode = null;
                    n.TreeView.Invalidate();
                }
            };
            MenuItem pack = new MenuItem("Pack");
            pack.Click += delegate(object sender, EventArgs e2)
            {
                if (VerifyPackPrecondition(
                    new List<TristateTreeNode>(
                        new TristateTreeNode[] { node }
                    )
                ))
                {
                    Title("Begin Packing...");
                    PackNode(node);
                    Title(GetWindowTitle());
                    Log("Pack done");
                }
            };
            cm.MenuItems.Add(delete);
            cm.MenuItems.Add(pack);
            cm.Show(changesView, new Point(e.X, e.Y));
        }

        /// <summary>
        /// Event handler for when a DragDrop operation completed on top of the changesview
        /// </summary>
        void changesView_DragDrop(object sender, DragEventArgs e)
        {
            //Check if we have a file/list of filfes
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {

                DataObject dataObject = (DataObject)e.Data;
                StringCollection dropList = dataObject.GetFileDropList();

                List<string> filePaths = new List<string>();
                foreach (string path in dropList)
                {
                    if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                        filePaths.AddRange(Util.GetAllChildFiles(path));//Directory.GetFiles(rootPath, "**", SearchOption.AllDirectories);
                    else
                        filePaths.Add(path);
                }
                if (filePaths.Count == 1) //test if its a project
                {
                    if (filePaths[0].ToLower().EndsWith(".rmproj"))
                    {
                        if (HasProjectChanged)
                            PromptSaveToClose();

                        //Load the project
                        LoadProject(filePaths[0]);
                        return;
                    }
                }

                //Iterate through all files
                StringQueryDialog nameQueryDialog = new StringQueryDialog("Type File Group Name:");
                nameQueryDialog.ShowDialog();
                if (nameQueryDialog.Value.Trim() == "")
                {
                    Log("Invalid name '{0}' given.  ".F(nameQueryDialog.Value.Trim()));
                    return;
                }
                TristateTreeNode topNode = new TristateTreeNode(nameQueryDialog.Value);
                topNode.HasCheckBox = true;
                for (int z = 0; z < filePaths.Count; z++)
                {
                    SetTaskbarProgress(z * 100 / filePaths.Count);
                    string filePath = filePaths[z].Replace("\\", "/");
                    //Console.WriteLine(filePath);

                    //ADD TO VIEW HERE
                    TristateTreeNode node;
                    topNode.Nodes.Add(
                        node = new TristateTreeNode(filePath)
                    );
                    node.HasCheckBox = true;

                    //changesView.Rows[rowIndex].Cells[CN_LOCALPATH].Style.Alignment = DataGridViewContentAlignment.MiddleRight;

                    //Split the path into pieces split by FSOs...  Search the RAF archives and see if we can link it to the raf path

                    string[] pathParts = filePath.Split("/");
                    RAFFileListEntry matchedEntry = null;
                    List<RAFFileListEntry> lastMatches = null;
                    bool done = false;

                    //Smart search insertion
                    for (int i = 1; i < pathParts.Length + 1 && !done; i++)
                    {
                        string[] searchPathParts = pathParts.SubArray(pathParts.Length - i, i);
                        string searchPath = String.Join("/", searchPathParts);
                        //Console.WriteLine(searchPath);
                        List<RAFFileListEntry> matches = new List<RAFFileListEntry>();
                        RAFArchive[] archives = rafArchives.Values.ToArray();
                        for (int j = 0; j < archives.Length; j++)
                        {
                            List<RAFFileListEntry> newmatches = archives[j].GetDirectoryFile().GetFileList().SearchFileEntries(searchPath);
                            matches.AddRange(newmatches);
                        }
                        if (matches.Count == 1)
                        {
                            matchedEntry = matches[0];
                            done = true;
                        }
                        else if (matches.Count == 0)
                        {
                            done = true;
                        }
                        else
                        {
                            lastMatches = matches;
                        }
                    }
                    if (matchedEntry == null)
                    {
                        if (lastMatches != null && lastMatches.Count > 0)
                        {
                            //Resolve ambiguity
                            FileEntryAmbiguityResolver ambiguityResolver = new FileEntryAmbiguityResolver(lastMatches.ToArray(), "!");
                            ambiguityResolver.ShowDialog();
                            RAFFileListEntry resolvedItem = (RAFFileListEntry)ambiguityResolver.SelectedItem;
                            if (resolvedItem != null)
                            {
                                matchedEntry = resolvedItem;
                            }
                        }
                    }
                    if (matchedEntry != null) //If it's still not resolved
                    {
                        node.Tag = new ChangesViewEntry(filePath, matchedEntry, node);
                        node.Nodes.Add(new TristateTreeNode("Local Path: " + filePath));
                        node.Nodes.Add(new TristateTreeNode("RAF Path: " + matchedEntry.RAFArchive.GetID() + "/" + matchedEntry.FileName));
                        node.Nodes[0].HasCheckBox = false;
                        node.Nodes[1].HasCheckBox = false;
                        //changesView.Rows[rowIndex].Cells[CN_RAFPATH].Value = matchedEntry.RAFArchive.GetID() + "/" + matchedEntry.FileName;
                        //changesView.Rows[rowIndex].Cells[CN_RAFPATH].Tag = matchedEntry;
                    }
                    else
                    {
                        Log("Unable to link file '" + filePath + "' to RAF Archive.  Please manually select RAF path");
                    }
                }
                changesView.Nodes.Add(topNode);
                changesView.Invalidate();
                SetTaskbarProgress(0);
            }
        }

        /// <summary>
        /// Event Handler for when a file is dragged over our ChangesView control
        /// </summary>
        void changesView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {   //If we are dragging a file over, say that a copy operation is "rammus: ok"
                e.Effect = DragDropEffects.Copy;
                Application.DoEvents();
            }            
        }

        /// <summary>
        /// When a cell changes, we tell the project that it has been changed, so an asterisk
        /// is stuck next to the project name
        /// </summary>
        void changesView_CurrentCellChanged(object sender, EventArgs e)
        {
            HasProjectChanged = true;
            UpdateProjectGUI();
        }

        /// <summary>
        /// When the RAF Content view has a dragover operation
        /// ???  Likely have an insert file operation, though
        /// I think this wouldn't be friendly to the user.
        /// 
        /// Dragging to the changesview is what the user wants more
        /// 
        /// Perhaps show a dialog if the user actually intends to
        /// add to the dragged over dialog
        /// </summary>
        void rafContentView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject && ((DataObject)e.Data).ContainsFileDropList())
            {
                e.Effect = DragDropEffects.Copy;

                rafContentView.Select();
                TreeNode hoveredNode = rafContentView.GetNodeAt(rafContentView.PointToClient(new Point(e.X, e.Y)));
                rafContentView.SelectedNode = hoveredNode;
                Application.DoEvents();
            }
        }

        /// <summary>
        /// When a RafContentView node is double clicked, extract and view its content
        /// </summary>
        void rafContentView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            RAFInMemoryFileSystemObject node = (RAFInMemoryFileSystemObject)e.Node;
            string nodeInternalPath = node.GetRAFPath();
            if (node.GetFSOType() == RAFFSOType.FILE)
            {
                //We have double clicked a file... find out what file it was
                List<RAFFileListEntry> entries = this.rafArchives[node.GetTopmostParent().Name]
                    .GetDirectoryFile().GetFileList().GetFileEntries();

                //Find the RAF File entry that corresponds to the clicked file...
                RAFFileListEntry entry = entries.Where(
                    (Func<RAFFileListEntry, bool>)delegate(RAFFileListEntry theEntry)
                    {
                        return theEntry.FileName == nodeInternalPath;
                    }
                ).First();

                //Now select a viewer to use for the file.
                if (entry.FileName.ToLower().EndsWith("inibin") ||
                    entry.FileName.ToLower().EndsWith("troybin"))
                {
                    try
                    {
                        new TextViewer(this.baseTitle + " - inibin/troybin view - " + nodeInternalPath,
                            new InibinFile().main(entry.GetContent())
                        ).Show();
                        return;
                    }
                    catch { Log("Error parsing INIBIN/TROYBIN file"); } //Fall back to textviewer
                }
                if (entry.FileSize < 10000 || //If > 200, ask, then continue
                       MessageBox.Show("This file is quite large ({0} bytes).  Sure you want to read it?".F(entry.FileSize), "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    if (entry.GetContent().All(c => c >= ' ' && c <= '~') || 
                        entry.FileName.ToLower().EndsWith("cfg") ||
                        entry.FileName.ToLower().EndsWith("ini") ||
                        entry.FileName.ToLower().EndsWith("txt") ||
                        entry.FileName.ToLower().EndsWith("log") ||
                        entry.FileName.ToLower().EndsWith("list") ||
                        entry.FileName.ToLower().EndsWith("xml")
                    ) //All content is displayable text, likely
                    {
                        new TextViewer(
                            ResolveRAFPathToEntry(node.GetTopmostParent().Text + "/" + node.GetRAFPath())
                        ).Show();
                    }
                    else //If all else fails, just use the binary viewer
                    {
                        new BinaryViewer(this.baseTitle + " - " + nodeInternalPath,
                            entry.GetContent()
                        ).Show();
                    }
                }
            }
        }
    }
}
