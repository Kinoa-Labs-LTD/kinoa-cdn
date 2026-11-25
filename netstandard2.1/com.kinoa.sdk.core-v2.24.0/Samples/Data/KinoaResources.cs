/// <summary>
///     Kinoa resources catalogue — the game's sellable / awardable items (weapons, boosters,
///     chests, cosmetics, event rewards, IAP goods) that mirror onto the Kinoa Dashboard as
///     resource templates. Pure data: one const per resource — the string literal IS the
///     resourceKey (byte-for-byte; must match ^[a-zA-Z][a-zA-Z0-9_-]*$); field metadata lives
///     in the structured doc comments below. Filled by the /kinoa resources --merge confirmation gate;
///     read by /kinoa dashboard-sync (Phase 7) to build the manifest's resources section.
///     Soft currency and consumables (coins, lives, energy) count too when the game
///     sells or awards them — the catalogue holds the awardable item; the player's
///     balance stays a player field.
///     Never add API calls, tokens, or Authorization headers here.
/// </summary>
public static class KinoaResources
{
    // One resource per const. Doc-comment grammar (read by /kinoa dashboard-sync):
    //   /// resource-name: <human-facing name>          (optional - defaults to the const identifier)
    //   /// resource-description: <short description>   (optional)
    //   /// field: NAME:TYPE[:ENUM_VALUES][:default=VALUE][:desc=TEXT]
    //   A field is minimally NAME:TYPE - default and desc are optional extras.
    //   TYPE is one of: number | string | boolean | date | enumeration
    //   (enumeration lists its allowed values comma-separated in the ENUM_VALUES token)
    //   A resource with NO field lines is a key-only item (granted as key + amount).
    //   The rebuild also emits a <ConstName>Fields companion class per resource
    //   (field-key consts + <FieldName>Values enum-value consts) for literal-free code.
    //
    //TODO: Add your resources here — run /kinoa resources --merge to discover and confirm them, e.g.:
    // /// resource-name: Legendary Sword
    // /// resource-description: Awarded for beating the final boss.
    // /// field: attack:number:default=100
    // /// field: rarity:enumeration:common,rare,epic
    // public const string LegendarySword = "legendary_sword";
}
