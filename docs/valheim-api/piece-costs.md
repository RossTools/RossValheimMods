# Piece build costs

Read at Valheim 1.0.20 (`Version.CurrentVersion`), from `assembly_valheim.dll`.

## `Piece.Requirement`

```csharp
public class Requirement
{
    [Header("Resource")]
    public ItemDrop m_resItem;
    public int m_amount = 1;
    public int m_extraAmountOnlyOneIngredient;
    [Header("Item")]
    public int m_amountPerLevel = 1;
    public bool m_upgraderResource;
    [Header("Piece")]
    public bool m_recover = true;
    ...
}

public Requirement[] m_resources = Array.Empty<Requirement>();
```

`m_resItem` is a real `ItemDrop` (the item prefab's component), not a name.

## Every build-time reader goes to the prefab's own `Piece`, every time

`PieceTable.GetSelectedPiece()` hands back the `Piece` on the piece
*prefab*, and nothing copies its `m_resources`:

- Placement, `Player.UpdatePlacement`:
  ```csharp
  Piece selectedPiece = m_buildPieces.GetSelectedPiece();
  ...
  if (m_noPlacementCost || HaveRequirements(selectedPiece, RequirementMode.CanBuild))
      if (TryPlacePiece(selectedPiece))
          ...
          ConsumeResources(selectedPiece.m_resources, 0);
  ```
- `Player.HaveRequirements(Piece piece, RequirementMode mode)` loops
  `piece.m_resources` directly.
- The build-menu cost tooltip, `Hud` (around `m_requirementItems`), indexes
  `piece.m_resources` each time it is drawn.

So assigning a new array to the prefab's `Piece.m_resources` at runtime
changes the cost from the next look at the hammer onward. ItemDrawers
relies on this to apply a server's synced recipe (issue #22).
