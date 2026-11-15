"""Üretilen bir projeye karşı canlı HTTP smoke testi (template deposu aracı; template çıktısına girmez).

Kullanım (uygulama çalışırken):
    python -I tests/live_smoke.py dev  http://localhost:5201 /api    0 <SeedAdminPassword>   # N-Tier, Development
    python -I tests/live_smoke.py prod http://localhost:5202 /api    0                       # N-Tier, Production
    python -I tests/live_smoke.py dev  http://localhost:5203 /api/v1 1 <SeedAdminPassword>   # Onion,  Development
Git Bash'te '/api' argümanının yola çevrilmemesi için MSYS_NO_PATHCONV=1 verin.
Production testi için veritabanı önce 'dotnet ef database update' ile kurulmalı ve Cors__AllowedOrigins__0=https://app.example.com verilmelidir.
"""
import json, sys, uuid, urllib.request, urllib.error
sys.stdout.reconfigure(encoding="utf-8")

mode, base, prefix, onion = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] == "1"
admin_pw = sys.argv[5] if len(sys.argv) > 5 else None
results = []


def req(method, path, body=None, token=None, headers=None, raw=None):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    r = urllib.request.Request(base + path, data=data, method=method)
    if data is not None:
        r.add_header("Content-Type", "application/json")
    if token:
        r.add_header("Authorization", "Bearer " + token)
    for k, v in (headers or {}).items():
        r.add_header(k, v)
    try:
        with urllib.request.urlopen(r) as resp:
            return resp.status, {k.lower(): v for k, v in resp.headers.items()}, resp.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, {k.lower(): v for k, v in e.headers.items()}, e.read().decode()


def check(name, ok, detail=""):
    results.append((name, ok, detail))


def data(body):
    return json.loads(body).get("data") if body else None


A = prefix
suffix = uuid.uuid4().hex[:8]

# --- ortak: kayıt ve kullanıcı
s, h, b = req("POST", f"{A}/auth/register", {"fullName": "Canlı Test", "userName": f"user{suffix}", "email": f"user{suffix}@example.com", "password": "Deneme12*"})
check("register → 201", s == 201, f"{s} {b[:120]}")
check("register Location '/' ile başlar", h.get("location", "").startswith("/"), h.get("location", ""))
check("register gövdesi {id}", s == 201 and "id" in (data(b) or {}), b[:100])
s, h, b = req("POST", f"{A}/auth/login", {"email": f"user{suffix}@example.com", "password": "Deneme12*"})
check("user login → 200", s == 200, str(s))
user_token = data(b)["accessToken"] if s == 200 else None
s, h, b = req("GET", f"{A}/auth/me", token=user_token)
check("yeni kullanıcı User rolünde", s == 200 and "User" in (data(b) or {}).get("roles", []), b[:150])
s, h, b = req("POST", f"{A}/categories", {"name": f"Kat-{suffix}"}, token=user_token)
check("User rolüyle kategori oluşturma → 403", s == 403, str(s))
s, h, b = req("POST", f"{A}/auth/refresh-token", {"accessToken": "abc", "refreshToken": "xyz"})
check("bozuk token ile refresh → 401", s == 401, f"{s} {b[:100]}")
s, h, b = req("GET", f"{A}/products/paged?pageNumber=2147483647&pageSize=100")
check("pageNumber=int.MaxValue → 200 boş sayfa", s == 200 and data(b)["items"] == [], f"{s} {b[:100]}")
s, h, b = req("POST", f"{A}/auth/login", raw=b'{ "email": 123, ')
check("bozuk JSON → 400, tip adı sızmaz", s == 400 and "Shop" not in b, f"{s} {b[:120]}")
me_codes = [req("GET", f"{A}/auth/me", token=user_token)[0] for _ in range(20)]
check("/auth/me 20 istekte 429 yok", 429 not in me_codes, str(sorted(set(me_codes))))
check("güvenlik başlıkları (nosniff, CSP, Server yok)",
      h.get("x-content-type-options") == "nosniff" and "content-security-policy" in h and "server" not in h, str({k: h.get(k) for k in ["x-content-type-options", "server"]}))

if mode == "dev":
    s, h, b = req("GET", "/health")
    check("/health → 200", s == 200, str(s))
    s, h, b = req("GET", "/health/ready")
    check("/health/ready → 200", s == 200, str(s))
    s, h, b = req("POST", f"{A}/auth/login", {"email": "admin@admin.com", "password": admin_pw})
    check("seed admin login → 200", s == 200, str(s))
    at = data(b)["accessToken"]
    s, h, b = req("POST", f"{A}/categories", {"name": f"Kat-{suffix}"}, token=at)
    check("Admin kategori oluşturma → 201", s == 201, f"{s} {b[:100]}")
    cid = data(b)["id"]
    s, h, b = req("GET", h.get("location"))
    check("Location adresi çalışır", s == 200, str(s))
    rv = data(b)["rowVersion"]
    s, h, b = req("PUT", f"{A}/categories/{cid}", {"name": f"Kat-{suffix}-a", "rowVersion": rv}, token=at)
    check("güncel RowVersion ile PUT → 204", s == 204, f"{s} {b[:100]}")
    s, h, b = req("PUT", f"{A}/categories/{cid}", {"name": f"Kat-{suffix}-b", "rowVersion": rv}, token=at)
    check("eski RowVersion ile PUT → 409", s == 409, f"{s} {b[:100]}")
    check("exception kaynaklı 409'da güvenlik başlığı", h.get("x-content-type-options") == "nosniff", str(h.get("x-content-type-options")))
    s, h, b = req("POST", f"{A}/products", {"name": f"Urun-{suffix}", "price": 999999, "stock": 5, "categoryId": cid}, token=at)
    check("ürün oluştur → 201", s == 201, f"{s} {b[:100]}")
    pid = data(b)["id"]
    s, h, b = req("GET", f"{A}/products/top-priced?count=5")
    check("top-priced (Dapper) yeni ürünü içerir", s == 200 and any(p["id"] == pid for p in data(b)), str(s))
    s, h, b = req("DELETE", f"{A}/categories/{cid}", token=at)
    check("ürünlü kategoriyi silme → 409", s == 409, str(s))
    s, h, b = req("GET", f"{A}/products", None)
    check("ürün listesi (cache doldurma) → 200", s == 200 and any(p["id"] == pid for p in data(b)), str(s))
    s, h, b = req("DELETE", f"{A}/products/{pid}", token=at)
    check("ürün sil → 204", s == 204, str(s))
    s, h, b = req("GET", f"{A}/products/top-priced?count=5")
    check("silinen ürün Dapper sonucunda yok (soft delete)", s == 200 and all(p["id"] != pid for p in data(b)), str(s))
    s, h, b = req("GET", f"{A}/products")
    check("silinen ürün listede yok (cache temizlendi)", s == 200 and all(p["id"] != pid for p in data(b)), str(s))
    o = json.loads(req("GET", "/openapi/v1.json")[2])
    ops = [(p, m, op) for p, v in o["paths"].items() for m, op in v.items()]
    login_ops = [op for p, m, op in ops if p.lower().endswith("/auth/login")]
    create_cat = [op for p, m, op in ops if p.lower().endswith("/categories") and m == "post"]
    check("OpenAPI: login uçunda kilit yok, kategori POST'ta var + 403",
          login_ops and not login_ops[0].get("security") and create_cat and create_cat[0].get("security") and "403" in create_cat[0].get("responses", {}), "")
    if onion:
        s, h, b = req("GET", "/api/v2/products")
        check("Onion: desteklenmeyen sürüm → 404", s == 404, str(s))
    codes = []
    for _ in range(17):
        s, h, b = req("POST", f"{A}/auth/login", {"email": "yok@example.com", "password": "Yanlis12*"})
        codes.append(s)
    check("auth limiti → 429 + Retry-After + ServiceResult gövdesi", s == 429 and "retry-after" in h and "errorMessage" in b, f"{s} {h.get('retry-after')} {b[:80]}")
else:
    s, h, b = req("GET", "/scalar/v1")
    check("Production: Scalar kapalı (404)", s == 404, str(s))
    s, h, b = req("GET", "/openapi/v1.json")
    check("Production: OpenAPI kapalı (404)", s == 404, str(s))
    s, h, b = req("OPTIONS", f"{A}/products", headers={"Origin": "http://localhost:3000", "Access-Control-Request-Method": "GET"})
    check("Production CORS: localhost:3000 izinli değil", "access-control-allow-origin" not in h, str(h.get("access-control-allow-origin")))
    s, h, b = req("OPTIONS", f"{A}/products", headers={"Origin": "https://app.example.com", "Access-Control-Request-Method": "GET"})
    check("Production CORS: tanımlı kaynak izinli", h.get("access-control-allow-origin") == "https://app.example.com", str(h.get("access-control-allow-origin")))
    s, h, b = req("GET", "/health/ready")
    check("Production /health/ready → 200", s == 200, str(s))

ok = sum(1 for r in results if r[1])
for name, passed, detail in results:
    print(("OK  " if passed else "FAIL") + "  " + name + ("" if passed else "  -> " + detail))
print(f"SONUC {ok}/{len(results)}")
