## Google Sheets Integration Assessment Plan

This plan assesses the feasibility of using Google Sheets for the estimator agent. Each phase delivers a verifiable outcome.

### Approach: MCP Server Integration

Instead of building a custom Google Sheets client, we'll use an existing MCP server: **[xing5/mcp-google-sheets](https://github.com/xing5/mcp-google-sheets)** (635+ stars).

**Why this approach:**
- Docker + SSE transport ready (same pattern as Paperless MCP)
- Service Account authentication (no OAuth browser flow)
- Provides all required operations: `create_spreadsheet`, `update_cells`, `get_sheet_data`, `batch_update_cells`
- Well-maintained and actively developed

---

### Phase 1: Google Cloud Setup & MCP Spike

**Goal:** Verify the MCP server works with our Google Cloud credentials.

**Tasks:**
1. Create Google Cloud project (or use existing)
2. Enable Google Sheets API and Google Drive API
3. Create Service Account with Editor role
4. Create a Google Drive folder and share it with the Service Account email
5. Run MCP server locally via Docker:
   ```bash
   docker run --rm -p 8000:8000 \
     -e HOST=0.0.0.0 -e PORT=8000 \
     -e CREDENTIALS_CONFIG=<base64-service-account-json> \
     -e DRIVE_FOLDER_ID=<folder-id> \
     ghcr.io/xing5/mcp-google-sheets:latest
   ```
6. Test with MCP client or curl:
   - Call `list_spreadsheets` to verify connection
   - Call `create_spreadsheet` to create a test sheet
   - Call `update_cells` to write data
   - Call `get_sheet_data` to read it back

**Verification:** MCP server creates a spreadsheet visible in Drive folder, writes and reads data successfully.

---

### Phase 2: Aspire Integration

**Goal:** Add Google Sheets MCP as an Aspire resource (like Paperless MCP).

**Tasks:**
1. Create `GoogleSheetsMcpExtensions.cs` in AppHost (follow `PaperlessMcpExtensions.cs` pattern)
2. Define `GoogleSheetsMcpResource` with:
   - `CREDENTIALS_CONFIG` parameter (base64 service account JSON)
   - `DRIVE_FOLDER_ID` parameter
   - HTTP endpoint for SSE transport
3. Add Aspire parameters for secrets (`google-sheets-credentials`, `google-drive-folder-id`)
4. Register in `AppHost.cs`:
   ```csharp
   var googleSheetsMcp = builder.AddGoogleSheetsMcp("google-sheets-mcp",
       googleSheetsCredentials, googleDriveFolderId);
   ```
5. Reference from ChatBro.Server

**Verification:** `dotnet run` in AppHost starts the MCP container, Aspire dashboard shows it healthy.

---

### Phase 3: MCP Client Integration

**Goal:** Connect ChatBro.Server to the Google Sheets MCP and expose tools to AI.

**Tasks:**
1. Create `GoogleSheetsMcpClient.cs` in ChatBro.Server (similar to `PaperlessMcpClient`)
2. Implement `GetToolsAsync()` to fetch available MCP tools
3. Add connection string injection via Aspire service discovery
4. Test tool invocation manually (create a sheet via the client)

**Verification:** `GoogleSheetsMcpClient.GetToolsAsync()` returns tools; calling `create_spreadsheet` creates a real sheet.

---

### Phase 4: Estimator Domain Agent

**Goal:** Wire up the estimator as a domain agent with Google Sheets tools.

**Tasks:**
1. Create `contexts/domains/estimator/` with:
   - `description.md` - what the agent does
   - `instructions.md` - how to use sheets for scoring items
2. Add `EstimatorDomainSettings` to `ChatSettings.Domains`
3. Implement `BuildEstimatorDomainAgentAsync()` in `AIAgentProvider`:
   - Inject Google Sheets MCP tools
   - Add `DateTimePlugin` for current date
4. Register in `BuildDomainAgentsAsync()` list
5. Update orchestrator to route estimator requests

**Verification:** Chat "I want to buy a bike" triggers estimator agent; it responds appropriately (may not create sheet yet without full instructions).

---

### Phase 5: End-to-End Validation

**Goal:** Validate the complete user flow from the original spec.

**Test scenarios:**
1. **Setup flow:** User describes search criteria → agent creates sheet with parameter columns and score formula → returns sheet link
2. **Add item flow:** User sends item link → agent extracts info, estimates parameters → adds row to sheet
3. **Summary flow:** User asks for comparison → agent reads sheet data → returns ranked items with pros/cons

**Verification:** All three scenarios work in Telegram chat with real Google Sheets.

---

### Risk Mitigation

| Risk | Mitigation |
|------|------------|
| MCP server doesn't support formulas | `update_cells` supports formula strings (e.g., `=SUM(A1:A10)`) - verify in Phase 1 |
| Service Account auth issues | Test credentials locally before Aspire integration |
| MCP server image availability | Can build locally from source if needed |
| Rate limits | Google Sheets API has generous limits (300 requests/minute) |

