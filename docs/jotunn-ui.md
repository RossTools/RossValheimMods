# Jotunn UI (GUIManager) notes

**Read the official docs first — don't re-derive what they already cover:**

- Guides & tutorials: <https://valheim-modding.github.io/Jotunn/>
- GUI tutorial specifically: <https://valheim-modding.github.io/Jotunn/tutorials/gui.html>
- API reference: <https://valheim-modding.github.io/Jotunn/api/index.html>
- Source: <https://github.com/Valheim-Modding/Jotunn>
- Example mod (most tutorials reference it): <https://github.com/Valheim-Modding/JotunnModExample>

This file records only the `GUIManager` behaviours that cost this repo real
iterations and that the pages above do not spell out. Verified against the
Jotunn version pinned here (2.30.1) by decompiling `Jotunn.dll`
(`ilspycmd -t Jotunn.Managers.GUIManager`) and by in-game testing during the
RossPortals build.

## `GUIManager.Create*` does NOT localize the text you give it

`CreateText`, `CreateButton`, `CreateInputField` (placeholder) set the widget's
`Text.text` to your string verbatim. A `$token` is shown **literally** unless
you localize it yourself — Jotunn does not run `Localization.Localize` over the
widgets it builds. Either pass a plain literal, or wrap the string in
`Localization.instance.Localize(...)` before handing it over.

This bit us twice on RossPortals: the panel showed raw `$rossportals_ok` etc.,
then raw `[piece_portal_target_none]` after that.

## Not every `$piece_portal_*`-looking token is vanilla

`$piece_portal_target_none` and `$piece_portal_tag_none` are **not** vanilla
tokens — they were defined by XPortal. An unregistered token renders as
`[token_without_dollar]`. Before reusing a "vanilla-looking" token, confirm it
exists (search a decompiled `Localization`/the language files); otherwise use a
literal. `$KEY_Use` and `$piece_portal_settag` are real vanilla tokens.

## `CreateScrollView`: two traps

`GUIManager.CreateScrollView(...)` builds a `ScrollRect` whose content is a
`VerticalLayoutGroup`, but:

- It only sets `childControlWidth = true` **when it draws a horizontal
  scrollbar**. With a vertical-only scroll view, rows keep their own width and
  overflow / don't fill the list. Set it yourself:
  `layout.childControlWidth = layout.childForceExpandWidth = true;`.
- Default `ScrollRect.scrollSensitivity` is ~35 — a long list crawls under the
  mouse wheel. Raise it (RossPortals uses 400): `scrollRect.scrollSensitivity = 400f;`.

Add rows to `scroll.GetComponentInChildren<ScrollRect>().content`.

## `CreateToggle` is the checkbox, not a checkbox+label

`GUIManager.CreateToggle(parent, width, height)` sizes the checkbox **graphic
itself** to `width x height` (Background + Checkmark). It is not a labelled
control — the template's own label is unusable at checkbox size. Draw your own
label next to it with `CreateText`, and set that label's `raycastTarget = false`
so it doesn't eat clicks meant for the box.

## Misc facts worth not re-learning

- It's legacy `UnityEngine.UI` (`Text`, `InputField`, `ScrollRect`, `Toggle`) —
  **not** TMP. Reference `UnityEngine.TextRenderingModule` for `Font`.
- Fonts: `GUIManager.Instance.AveriaSerif` / `AveriaSerifBold`.
- Parent custom UI under `GUIManager.CustomGUIFront` (a static `GameObject`).
- `GUIManager.BlockInput(true/false)` while a modal panel is open; `IsHeadless()`
  guards against building any of this on a dedicated server.

## Admin-only config sync raises `SettingChanged` (read at Jotunn 2.30.2)

Not UI, but a Jotunn trap worth keeping with the others. `SynchronizationManager`
writes every synced value through `ConfigEntryBase.BoxedValue`, which raises
BepInEx's `SettingChanged`:

- on joining a server as a client: `SetToDefaultConfigEntries` sets each
  admin-only entry to its default (`val.BoxedValue = val.DefaultValue`), then
  `ApplyConfigZPackage` writes the server's value
  (`val.BoxedValue = TomlTypeConverter.ConvertToValue(...)`) and raises
  `OnConfigurationSynchronized`;
- on leaving, `ResetAdminConfigs` restores the values it cached from the local
  file (`localValue.Key.BoxedValue = localValue.Value`).

So anything read once at startup from an admin-only entry, such as a piece's
cost baked at registration, silently keeps the player's *local* value on a
server. Subscribe to the entry's `SettingChanged` and re-apply instead.
