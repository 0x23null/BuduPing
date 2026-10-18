# Pair Code — Thuật toán mã cặp BuduPing (dùng chung cho Windows + Android)

Mã cặp là "chìa khoá" duy nhất để 2 app nhận nhau. Không đăng nhập, không tài khoản.
CẢ HAI PHÍA (Windows và Android) PHẢI implement đúng y hệt spec này.

## 1. Bảng chữ cái (Crockford Base32 rút gọn)

```
0123456789ABCDEFGHJKMNPQRSTVWXYZ
```

- 32 ký tự. ĐÃ LOẠI: `I`, `L`, `O`, `U` (dễ nhầm khi đọc/gõ).
- Thứ tự trên là thứ tự chỉ số: `0`=0, `1`=1, ..., `9`=9, `A`=10, ..., `H`=17, `J`=18, `K`=19, `M`=20, ..., `Z`=31.

## 2. Cấu tạo mã

- Tổng cộng **8 ký tự**: **7 ký tự payload** (random đều 0..31) + **1 ký tự checksum** ở cuối.
- Checksum = `alphabet[ (tổng chỉ số của 7 ký tự payload) mod 32 ]`.
- Hiển thị cho người dùng: `XXXX-XXXX` (4 đầu, dấu `-`, 4 cuối). Ví dụ hợp lệ: `QJNT-EBJ3`.
- `pairId` dùng trong Firebase = 8 ký tự **không dấu gạch**: ví dụ `QJNTEBJ3`.

## 3. Chuẩn hoá input (bên nhập mã, phía Android)

Trước khi validate, áp dụng theo đúng thứ tự:
1. Viết hoa toàn bộ.
2. `O` → `0`; `I` → `1`; `L` → `1`.
3. Giữ lại chỉ ký tự thuộc bảng chữ cái trên (bỏ `-`, khoảng trắng, ký tự lạ).

## 4. Validate

- Sau chuẩn hoá phải đúng **8 ký tự**.
- Tính lại checksum từ 7 ký tự đầu; ký tự thứ 8 phải khớp.
- Sai bất kỳ điều kiện nào → báo lỗi ngay tại chỗ ("mã không đúng, check lại phía Windows").

## 5. Tham chiếu (Python, chỉ để đối chiếu)

```python
ALPHA = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"

def normalize(s: str) -> str:
    s = s.upper().replace("O", "0").replace("I", "1").replace("L", "1")
    return "".join(c for c in s if c in ALPHA)

def is_valid_pair_code(s: str) -> bool:
    raw = normalize(s)
    if len(raw) != 8:
        return False
    checksum = ALPHA[sum(ALPHA.index(c) for c in raw[:7]) % 32]
    return raw[7] == checksum

def make_pair_code() -> str:
    import secrets
    payload = [secrets.randbelow(32) for _ in range(7)]
    code = "".join(ALPHA[i] for i in payload)
    code += ALPHA[sum(payload) % 32]
    return code  # raw 8 ký tự; hiển thị thì chèn "-" sau ký tự thứ 4
```

Ví dụ hợp lệ: `QJNT-EBJ3` (raw `QJNTEBJ3`) — đã tự kiểm chứng pass.
