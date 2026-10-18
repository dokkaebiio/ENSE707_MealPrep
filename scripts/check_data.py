"""Data sanity gate for MealPrep (run by CI, or locally: python3 scripts/check_data.py).

Fails (exit 1) if the grocery or deals data would make the app show wrong
prices or wrong totals: bad JSON, duplicate ids, missing names, prices that
are not positive, unknown supermarkets, or deals pointing at items that
do not exist.
"""
import json
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ITEMS = ROOT / "wwwroot" / "data" / "items.json"
DEALS = ROOT / "wwwroot" / "data" / "deals.json"
KNOWN_STORES = {"PAK'nSAVE", "New World", "Woolworths", "Generic"}

errors = []


def get(obj, key):
    # System.Text.Json reads keys case-insensitively, so accept either case here too.
    for k, v in obj.items():
        if k.lower() == key.lower():
            return v
    return None


def load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        errors.append(f"{path.name}: cannot parse JSON ({exc})")
        return []


items = load(ITEMS)
deals = load(DEALS)

item_ids = set()
for i, item in enumerate(items):
    iid = get(item, "id")
    where = f"items.json[{i}] (id={iid})"
    if not isinstance(iid, int):
        errors.append(f"{where}: id missing or not an integer")
    elif iid in item_ids:
        errors.append(f"{where}: duplicate id")
    item_ids.add(iid)

    if not str(get(item, "name") or "").strip():
        errors.append(f"{where}: name is empty")
    if not str(get(item, "category") or "").strip():
        errors.append(f"{where}: category is empty")

    prices = get(item, "prices") or []
    if not prices:
        errors.append(f"{where}: no prices")
    stores_seen = set()
    for p in prices:
        store, price = get(p, "supermarket"), get(p, "price")
        if store not in KNOWN_STORES:
            errors.append(f"{where}: unknown supermarket '{store}'")
        if store in stores_seen:
            errors.append(f"{where}: duplicate price for '{store}'")
        stores_seen.add(store)
        if not isinstance(price, (int, float)) or price <= 0 or price > 1000:
            errors.append(f"{where}: price {price!r} at {store} is not a sensible positive amount")

deal_ids = set()
for i, deal in enumerate(deals):
    did = get(deal, "id")
    where = f"deals.json[{i}] (id={did})"
    if did in deal_ids:
        errors.append(f"{where}: duplicate id")
    deal_ids.add(did)
    if get(deal, "itemId") not in item_ids:
        errors.append(f"{where}: itemId {get(deal, 'itemId')} does not exist in items.json")
    if get(deal, "supermarket") not in KNOWN_STORES:
        errors.append(f"{where}: unknown supermarket '{get(deal, 'supermarket')}'")
    price = get(deal, "dealPrice")
    if not isinstance(price, (int, float)) or price <= 0:
        errors.append(f"{where}: dealPrice {price!r} is not positive")
    try:
        if datetime.fromisoformat(get(deal, "expiresOn")) < datetime.fromisoformat(get(deal, "updatedAt")):
            errors.append(f"{where}: expires before it was updated")
    except Exception:  # noqa: BLE001
        errors.append(f"{where}: expiresOn/updatedAt is not a valid date")

print(f"Checked {len(items)} items and {len(deals)} deals.")
if errors:
    print(f"FAILED with {len(errors)} problem(s):")
    for e in errors:
        print("  -", e)
    sys.exit(1)
print("Data sanity check passed.")
