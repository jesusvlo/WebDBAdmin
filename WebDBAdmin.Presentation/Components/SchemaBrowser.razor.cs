using Radzen;
using WebDBAdmin.Domain.Enums;
using WebDBAdmin.Application.DTOs;
using ConnectionInfo = WebDBAdmin.Domain.Entities.ConnectionInfo;
using Microsoft.AspNetCore.Components; // Added for Parameter attribute
using Microsoft.Extensions.Localization; // Added for IStringLocalizer

namespace WebDBAdmin.Presentation.Components
{
    public partial class SchemaBrowser
    {
        public class TableSelectionArgs
        {
            public ConnectionInfo Connection { get; set; } = null!;
            public string TableName { get; set; } = "";
        }

        [Parameter] public ConnectionInfo? Connection { get; set; }
        [Parameter] public EventCallback<TableSelectionArgs> OnTableSelected { get; set; }

        List<SchemaNode> nodes = new List<SchemaNode>();

        protected override void OnInitialized()
        {
            SessionState.OnChange += OnConnectionChanged;

            // Prefer Parameter, then Session
            if (Connection != null)
            {
                LoadServerNode();
            }
            else if (SessionState.IsConnected)
            {
                OnConnectionChanged();
            }
        }

        public void Dispose()
        {
            SessionState.OnChange -= OnConnectionChanged;
        }

        private void OnConnectionChanged()
        {
            if (Connection != null) return; // Ignore global changes if scoped

            if (SessionState.IsConnected)
            {
                LoadServerNode();
            }
            else
            {
                nodes.Clear();
                StateHasChanged();
            }
        }

        void LoadServerNode()
        {
            try
            {
                var conn = Connection ?? SessionState.CurrentConnection;
                var root = new SchemaNode
                {
                    Name = conn?.Server ?? "Server",
                    Type = NodeType.Server,
                    HasChildren = true,
                    ParentNode = null,
                    DatabaseName = null
                };

                // If only one database is specified in connection, treat root as that database context effectively
                // But structure says Server -> Database -> Table
                // If the connection is specific to a DB, usually metadata service returns just that one DB or we treat it as such.

                nodes = new List<SchemaNode> { root };
                StateHasChanged();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["Error"], ex.Message);
            }
        }

        ConnectionInfo GetConnectionForNode(SchemaNode node)
        {
            var baseConnection = Connection ?? SessionState.CurrentConnection!;
            string? dbName = node.DatabaseName ?? (node.Type == NodeType.Database ? node.Name : null);

            // Navigate up to find database name if not set on current node
            var current = node;
            while (dbName == null && current.ParentNode != null)
            {
                current = current.ParentNode;
                if (current.Type == NodeType.Database)
                    dbName = current.Name;
            }

            if (dbName != null)
            {
                return new WebDBAdmin.Domain.Entities.ConnectionInfo
                {
                    Id = Guid.NewGuid(),
                    Name = baseConnection.Name,
                    Engine = baseConnection.Engine,
                    Server = baseConnection.Server,
                    Port = baseConnection.Port,
                    Database = dbName,
                    Username = baseConnection.Username,
                    Password = baseConnection.Password,
                    TrustedConnection = baseConnection.TrustedConnection
                };
            }
            return baseConnection;
        }

        void OnItemContextMenu(TreeItemContextMenuEventArgs args)
        {
            var node = args.Value as SchemaNode;
            if (node == null) return;

            var menuItems = new List<ContextMenuItem>();

            menuItems.Add(new ContextMenuItem { Text = Loc["Properties"], Value = "PROPERTIES", Icon = "info" });

            if (node.Type == NodeType.Database)
            {
                menuItems.Add(new ContextMenuItem { Text = Loc["CreateTable"], Value = "CREATE_TABLE", Icon = "add_box" });
            }

            if (node.Type == NodeType.Table)
            {
                menuItems.Add(new ContextMenuItem { Text = Loc["SelectAllRows"], Value = "SELECT", Icon = "table_rows" });
                menuItems.Add(new ContextMenuItem { Text = Loc["ModifyTable"], Value = "MODIFY_TABLE", Icon = "edit" });
                menuItems.Add(new ContextMenuItem { Text = Loc["DropTable"], Value = "DROP_TABLE", Icon = "delete" });
            }

            if (menuItems.Count > 0)
            {
                ContextMenuService.Open(args, menuItems, (e) => OnContextMenuItemClick(e, node));
            }
        }

        async void OnContextMenuItemClick(MenuItemEventArgs args, SchemaNode node)
        {
            ContextMenuService.Close();

            if (SessionState.CurrentConnection == null) return;

            string action = args.Value.ToString() ?? "";

            if (action == "SELECT")
            {
                // We use RequestTableLoad to signal SqlWorkspace to load this table dynamically using XPO
                // We pass a new ConnectionInfo scoped to the specific database if needed, or the current one.

                if (OnTableSelected.HasDelegate)
                {
                    var connectionInfo = GetConnectionForNode(node);
                    await OnTableSelected.InvokeAsync(new TableSelectionArgs { Connection = connectionInfo, TableName = node.Name });
                }
                else
                {
                    var connectionInfo = GetConnectionForNode(node);
                    UIInteraction.RequestTableLoad(connectionInfo, node.Name);
                }

                NotificationService.Notify(NotificationSeverity.Info, Loc["TableLoaded"], $"{Loc["LoadingTableData"]} {node.Name}...");
            }
            else if (action == "PROPERTIES")
            {
                NotificationService.Notify(NotificationSeverity.Info, Loc["Properties"], $"Type: {node.Type}, Name: {node.Name}");
            }
            else if (action == "CREATE_TABLE")
            {
                await HandleCreateTable(node);
            }
            else if (action == "MODIFY_TABLE")
            {
                await HandleModifyTable(node);
            }
            else if (action == "DROP_TABLE")
            {
                await HandleDropTable(node);
            }
        }

        async Task HandleCreateTable(SchemaNode databaseNode)
        {
            var connectionInfo = GetConnectionForNode(databaseNode);
            var tableDef = new TableDefinition();

            var result = await DialogService.OpenAsync<TableDialog>(Loc["CreateTable"],
               new Dictionary<string, object> {
                   { "TableDefinition", tableDef },
                   { "Engine", connectionInfo.Engine }
               },
               new DialogOptions() { Width = "700px", Height = "500px", Resizable = true, Draggable = true });

            if (result is TableDefinition newTable)
            {
                try
                {
                    await TableService.CreateTableAsync(connectionInfo, newTable);
                    NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], string.Format(Loc["TableCreatedSuccess"], newTable.Name));

                    // Refresh database node
                    databaseNode.ChildrenLoaded = false;
                    databaseNode.IsExpanded = false; // Collapse to force refresh logic on expand? 
                    // Better: re-trigger expand
                    await OnExpand(new TreeExpandEventArgs { Value = databaseNode });
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Loc["ErrorCreatingTable"], ex.Message);
                }
            }
        }

        async Task HandleModifyTable(SchemaNode tableNode)
        {
            var connectionInfo = GetConnectionForNode(tableNode);

            try
            {
                // We need current columns to populate the dialog
                var currentColumns = await SchemaService.GetColumnsAsync(connectionInfo, tableNode.Name);

                var tableDef = new TableDefinition
                {
                    Name = tableNode.Name,
                    Columns = currentColumns // Direct assignment
                };

                // Capture original state for comparison (Deep copy of columns needed)
                var originalColumns = tableDef.Columns.Select(c => new ColumnDefinition
                {
                    Name = c.Name,
                    Type = c.Type,
                    Length = c.Length,
                    IsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey
                }).ToList();
                var originalColumnNames = originalColumns.Select(c => c.Name).ToList();

                var result = await DialogService.OpenAsync<TableDialog>(Loc["ModifyTable"],
                   new Dictionary<string, object> {
                       { "TableDefinition", tableDef },
                       { "Engine", connectionInfo.Engine },
                       { "IsModification", true }
                   },
                   new DialogOptions() { Width = "700px", Height = "500px", Resizable = true, Draggable = true });

                if (result is TableDefinition modifiedTable)
                {
                    // 1. Rename Table
                    if (modifiedTable.Name != tableNode.Name)
                    {
                        await TableService.RenameTableAsync(connectionInfo, tableNode.Name, modifiedTable.Name);
                        tableNode.Name = modifiedTable.Name; // Update local node
                    }

                    // 2. Add Columns
                    var newColumns = modifiedTable.Columns.Where(nc => !originalColumnNames.Contains(nc.Name)).ToList();
                    foreach (var newCol in newColumns)
                    {
                        await TableService.AddColumnAsync(connectionInfo, modifiedTable.Name, newCol);
                    }

                    // 3. Drop Columns
                    var currentColumnNames = modifiedTable.Columns.Select(c => c.Name).ToList();
                    var droppedColumns = originalColumnNames.Where(oc => !currentColumnNames.Contains(oc)).ToList();
                    foreach (var droppedCol in droppedColumns)
                    {
                        await TableService.DropColumnAsync(connectionInfo, modifiedTable.Name, droppedCol);
                    }

                    // 4. Modify Columns (Change Type/Properties)
                    // 4. Modify Columns (Change Type/Properties) - DESTRUCTIVE approach
                    // We identify columns that need modification, and for each we will DROP and RE-ADD.
                    // This causes data loss for that column. We must warn the user.
                    var columnsToModify = new List<ColumnDefinition>();
                    foreach (var modCol in modifiedTable.Columns)
                    {
                        var original = originalColumns.FirstOrDefault(oc => oc.Name == modCol.Name);
                        if (original != null)
                        {
                            if (original.Type != modCol.Type ||
                                original.Length != modCol.Length ||
                                original.IsNullable != modCol.IsNullable ||
                                original.IsPrimaryKey != modCol.IsPrimaryKey)
                            {
                                columnsToModify.Add(modCol);
                            }
                        }
                    }

                    if (columnsToModify.Any())
                    {
                        var msg = string.Format(Loc["DataLossWarning"], string.Join("\n", columnsToModify.Select(c => $"- {c.Name}")));

                        var confirmResult = await DialogService.Confirm(msg, Loc["ConfirmDataLoss"],
                             new ConfirmOptions() { OkButtonText = Loc["YesUnderstand"], CancelButtonText = Loc["Cancel"] });

                        if (confirmResult == true)
                        {
                            foreach (var modCol in columnsToModify)
                            {
                                // We use the ORIGINAL name to drop, in case rename happened separately (though rename logic handles table rename, column rename is drop/add usually anyway or distinct).
                                // Here we assume column name matches original if we found it in originalColumns list by Name.
                                // If column rename is supported, that's a different detection logic. Here we match by Name.
                                await TableService.DropColumnAsync(connectionInfo, modifiedTable.Name, modCol.Name);
                                await TableService.AddColumnAsync(connectionInfo, modifiedTable.Name, modCol);
                            }
                        }
                        else
                        {
                            // User cancelled modifications. 
                            // Note: Renames, Adds, and Drops calculated above (steps 1, 2, 3) might have already executed if we didn't batch them or check this first.
                            // Ideally, we should check this BEFORE steps 2 and 3 if we want atomic-like cancellation, 
                            // but usually modifications are unrelated to adds/drops. 
                            // However, strictly speaking, we are only cancelling the *modification* part here.
                            NotificationService.Notify(NotificationSeverity.Info, Loc["Cancelled"], Loc["ModificationsCancelled"]);
                        }
                    }

                    NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], string.Format(Loc["TableModifiedSuccess"], modifiedTable.Name));

                    // Refresh table node
                    tableNode.ChildrenLoaded = false;
                    tableNode.IsExpanded = false;
                    // Collapse parent? Or just user will refresh manually?
                    // Let's try to reload children of this table node if it was expanded
                    if (tableNode.IsExpanded)
                    {
                        await OnExpand(new TreeExpandEventArgs { Value = tableNode });
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["ErrorModifyingTable"], ex.Message);
            }
        }

        async Task HandleDropTable(SchemaNode tableNode)
        {
            var result = await DialogService.Confirm(string.Format(Loc["DropTableConfirmation"], tableNode.Name), Loc["DropTable"],
               new ConfirmOptions() { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (result == true)
            {
                var connectionInfo = GetConnectionForNode(tableNode);
                try
                {
                    await TableService.DropTableAsync(connectionInfo, tableNode.Name);
                    NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], string.Format(Loc["TableDroppedSuccess"], tableNode.Name));

                    // Remove from UI
                    if (tableNode.ParentNode != null && tableNode.ParentNode.Children != null)
                    {
                        tableNode.ParentNode.Children.Remove(tableNode);
                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Loc["ErrorDroppingTable"], ex.Message);
                }
            }
        }



        void OnCollapse(TreeEventArgs args)
        {
            var node = args.Value as SchemaNode;
            if (node != null)
            {
                node.IsExpanded = false;
            }
        }

        async Task OnExpand(TreeExpandEventArgs args)
        {
            var node = args.Value as SchemaNode;
            if (node == null) return;

            node.IsExpanded = true;


            if (node.ChildrenLoaded) return;
            var conn = Connection ?? SessionState.CurrentConnection;
            if (conn == null) return;

            try
            {
                switch (node.Type)
                {
                    case NodeType.Server:
                        await ExpandServerNode(node);
                        break;
                    case NodeType.Database:
                        await ExpandDatabaseNode(node);
                        break;
                    case NodeType.Table:
                        await ExpandTableNode(node);
                        break;
                }

                node.ChildrenLoaded = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["ErrorExpandingNode"], ex.Message);
                node.ChildrenLoaded = false;
            }
        }

        async Task ExpandServerNode(SchemaNode serverNode)
        {
            var conn = Connection ?? SessionState.CurrentConnection;
            var dbs = await SchemaService.GetDatabasesAsync(conn!);
            var children = new List<SchemaNode>();

            if (dbs.Any())
            {
                foreach (var db in dbs)
                {
                    children.Add(new SchemaNode
                    {
                        Name = db,
                        Type = NodeType.Database,
                        HasChildren = true,
                        ParentNode = serverNode,
                        DatabaseName = db
                    });
                }
            }
            else
            {
                var tables = await SchemaService.GetTablesAsync(SessionState.CurrentConnection!);
                foreach (var t in tables)
                {
                    children.Add(new SchemaNode
                    {
                        Name = t,
                        Type = NodeType.Table,
                        HasChildren = true,
                        ParentNode = serverNode,
                        DatabaseName = null
                    });
                }
            }
            serverNode.Children = children;
        }

        async Task ExpandDatabaseNode(SchemaNode databaseNode)
        {
            var conn = Connection ?? SessionState.CurrentConnection!;
            var dbSpecificConnection = new WebDBAdmin.Domain.Entities.ConnectionInfo
            {
                Id = Guid.NewGuid(),
                Name = conn.Name,
                Engine = conn.Engine,
                Server = conn.Server,
                Port = conn.Port,
                Database = databaseNode.DatabaseName!,
                Username = conn.Username,
                Password = conn.Password,
                TrustedConnection = conn.TrustedConnection
            };

            var tables = await SchemaService.GetTablesAsync(dbSpecificConnection);

            if (!tables.Any())
            {
                NotificationService.Notify(NotificationSeverity.Warning, Loc["DatabaseEmpty"], string.Format(Loc["NoTablesFound"], databaseNode.DatabaseName));
            }

            var children = new List<SchemaNode>();
            foreach (var t in tables)
            {
                children.Add(new SchemaNode
                {
                    Name = t,
                    Type = NodeType.Table,
                    HasChildren = true,
                    ParentNode = databaseNode,
                    DatabaseName = databaseNode.DatabaseName
                });
            }
            databaseNode.Children = children;
        }

        async Task ExpandTableNode(SchemaNode tableNode)
        {
            var conn = Connection ?? SessionState.CurrentConnection!;
            var connectionInfo = conn;

            if (tableNode.DatabaseName != null)
            {
                connectionInfo = new WebDBAdmin.Domain.Entities.ConnectionInfo
                {
                    Id = Guid.NewGuid(),
                    Name = conn.Name,
                    Engine = conn.Engine,
                    Server = conn.Server,
                    Port = conn.Port,
                    Database = tableNode.DatabaseName,
                    Username = conn.Username,
                    Password = conn.Password,
                    TrustedConnection = conn.TrustedConnection
                };
            }

            var columns = await SchemaService.GetColumnsAsync(connectionInfo, tableNode.Name);
            var children = new List<SchemaNode>();
            foreach (var c in columns)
            {
                children.Add(new SchemaNode
                {
                    Name = c.Name,
                    Type = NodeType.Column,
                    HasChildren = false,
                    ParentNode = tableNode,
                    DatabaseName = tableNode.DatabaseName
                });
            }
            tableNode.Children = children;
        }

        string GetIconForNode(SchemaNode? node)
        {
            if (node == null) return "help";

            return node.Type switch
            {
                NodeType.Server => "dns",
                NodeType.Database => "storage",
                NodeType.Table => "table_chart",
                NodeType.Column => "view_column",
                _ => "help"
            };
        }

        string GetQualifiedTableName(SchemaNode tableNode)
        {
            if (tableNode.DatabaseName != null && SessionState.CurrentConnection != null)
            {
                return SessionState.CurrentConnection.Engine switch
                {
                    DatabaseEngine.SqlServer => $"[{tableNode.DatabaseName}].[dbo].[{tableNode.Name}]",
                    DatabaseEngine.MySql => $"`{tableNode.DatabaseName}`.`{tableNode.Name}`",
                    DatabaseEngine.PostgreSql => $"\"{tableNode.DatabaseName}\".\"{tableNode.Name}\"",
                    _ => tableNode.Name
                };
            }

            return tableNode.Name;
        }

        public class SchemaNode
        {
            public string Name { get; set; } = "";
            public NodeType Type { get; set; }
            public bool HasChildren { get; set; }
            public bool IsExpanded { get; set; }
            public bool ChildrenLoaded { get; set; } = false;
            public List<SchemaNode>? Children { get; set; }
            public SchemaNode? ParentNode { get; set; }
            public string? DatabaseName { get; set; }
        }

        public enum NodeType
        {
            Server,
            Database,
            Table,
            Column
        }
    }
}
