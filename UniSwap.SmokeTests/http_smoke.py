"""Run against a disposable UniSwap API in Development with migrations applied."""
from concurrent.futures import ThreadPoolExecutor
import json
import http.client
import os
import secrets
import struct
import time
import zlib
import urllib.error
import urllib.request
import urllib.parse

base = os.environ["UniSwapTests__ApiUrl"].rstrip("/")

def request(method, path, expected, body=None, token=None, content_type="application/json", raw_response=False):
    headers = {"Content-Type": content_type, "Accept": "*/*"}
    if token: headers['Authorization']='Bearer '+token
    req = urllib.request.Request(base+path, data=body if isinstance(body, bytes) else json.dumps(body).encode() if body is not None else None, method=method, headers=headers)
    try: result = urllib.request.urlopen(req, timeout=5)
    except urllib.error.HTTPError as e: result = e
    with result:
        raw = result.read()
        if isinstance(expected, tuple):
            assert result.code in expected, (method, path, result.code, expected)
            return result.code
        assert result.code == expected, (method, path, result.code, expected)
        return raw if raw_response else json.loads(raw) if raw else None

schema = request("GET", "/openapi/v1.json", 200)
assert schema["components"]["securitySchemes"]["Bearer"]["scheme"] == "bearer"
assert schema["paths"]["/api/users/me"]["get"]["security"] == [{"Bearer": []}]
assert not schema["paths"]["/api/listings"]["get"].get("security")
suffix = secrets.token_hex(8)
alice_email = f'alice-{suffix}@example.test'
bob_email = f'bob-{suffix}@example.test'
auth='/api/auth'
listings='/api/listings'
profile='/api/users/me'
user_password=secrets.token_hex(16)
listing={'title':'A textbook','description':'Course material','category':'Books','condition':'Used'}
request('POST', listings, 401, listing)
request('POST', auth+'/register', 400, {'name':'','email':'invalid','password':'short'})
alice=request('POST', auth+'/register', 201, {'name':'Alice','email':alice_email.upper(),'password':user_password,'role':'Admin'})
assert alice['role']=='User'
duplicate = request('POST', auth+'/register', 409, {'name':'Duplicate','email':alice_email,'password':user_password})
assert duplicate["code"] == "email_already_registered"
bob=request('POST', auth+'/register', 201, {'name':'Bob','email':bob_email,'password':user_password})
request('POST', auth+'/login', 401, {'email':alice_email,'password':'wrong password'})
def login(email, password):
    return request('POST', auth+'/login', 200, {'email':email,'password':password})['accessToken']
alice_token=login(alice_email.upper(),user_password)
another_session=login(alice_email,user_password)
bob_token=login(bob_email,user_password)
admin_token = None
if os.environ.get('AdminBootstrap__Email') and os.environ.get('AdminBootstrap__Password'):
    admin_token=login(os.environ['AdminBootstrap__Email'], os.environ['AdminBootstrap__Password'])
    assert request('GET', profile, 200, token=admin_token)['role']=='Admin'
changed=request('PUT', profile, 200, {'name':'Alice Updated','role':'Admin','id':bob['id']},alice_token)
assert changed['id']==alice['id'] and changed['role']=='User' and changed['name']=='Alice Updated'
item=request('POST', listings, 200, dict(listing,ownerId=bob['id']),alice_token)['id']
item_path=listings+'/'+item
owner_path = f"{listings}/mine/{item}"
assert request("GET", owner_path, 401)["code"] == "unauthorized"
assert request("GET", owner_path, 404, token=bob_token)["code"] == "not_found"
assert request("GET", owner_path, 200, token=alice_token)["status"] == "Draft"
request("GET", item_path, 404, token=alice_token)
request('POST', item_path+'/publish',403,token=bob_token)
request('POST', item_path+'/publish',204,token=alice_token)
assert request('GET',item_path,200)['ownerId']==alice['id']
request('PUT',item_path,403,listing,bob_token)
request('DELETE',item_path,403,token=bob_token)
assert request('PUT',item_path,200,dict(listing, description=f'Unique material {suffix}'),alice_token)['id']==item
result=request('GET',listings+f'?search=MATERIAL%20{suffix}&category=books&condition=used&available=true',200)
assert [x['id'] for x in result['items']]==[item]
assert request('GET',listings+f'?search={suffix}&available=false',200)['totalCount']==0
assert request('GET',listings+f'?search={suffix}&category=Electronics',200)['totalCount']==0
assert request("GET", listings + "?pageSize=101", 400)["code"] == "validation_failed"
assert request("GET", listings + "?page=abc", 400)["code"] == "validation_failed"
owned=request('GET',listings+'/mine',200,token=alice_token)
assert [x['id'] for x in owned['items']]==[item]
temporary=request('POST',listings,200,dict(listing,title=f'Delete me {suffix}'),alice_token)['id']
temporary_path=listings+'/'+temporary
image_list = temporary_path + "/images"
def png_chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

png = (b"\x89PNG\r\n\x1a\n"
       + png_chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 2, 0, 0, 0))
       + png_chunk(b"IDAT", zlib.compress(b"\0\x70\x90\xff"))
       + png_chunk(b"IEND", b""))

def upload(content, expected, token=alice_token):
    boundary = "uniswap-smoke"
    head = (f"--{boundary}\r\n"
            'Content-Disposition: form-data; name="file"; filename="picture.png"\r\n'
            'Content-Type: image/png\r\n\r\n').encode()
    body = head + content + f"\r\n--{boundary}--\r\n".encode()
    return request("POST", image_list, expected, body, token,
                   content_type=f"multipart/form-data; boundary={boundary}")

request("GET", image_list, 404)
upload(png, 403, bob_token)
assert upload(b"<svg/>", 400)["code"] == "invalid_image"
upload(b"\x89PNG\r\n\x1a\n", 400)
exact_limit_image = upload(png + b"\0" * (5 * 1024 * 1024 - len(png)), 201)
assert exact_limit_image["sizeBytes"] == 5 * 1024 * 1024
request("DELETE", image_list + "/" + exact_limit_image["id"], 204, token=alice_token)
assert upload(png + b"\0" * (5 * 1024 * 1024), 413)["code"] == "payload_too_large"

# A request exceeding the multipart limit must return a readable 413 response,
# both with Content-Length and with HTTP/1.1 chunked transfer encoding.
oversized_body = (
    b'--oversize\r\nContent-Disposition: form-data; name="file"; filename="image.png"\r\n'
    b'Content-Type: image/png\r\n\r\n' + png + b'\0' * (5 * 1024 * 1024 + 100000)
    + b'\r\n--oversize--\r\n'
)
assert request("POST", image_list, 413, oversized_body, alice_token,
               content_type="multipart/form-data; boundary=oversize")["code"] == "payload_too_large"
url = urllib.parse.urlsplit(base)
connection_class = http.client.HTTPSConnection if url.scheme == "https" else http.client.HTTPConnection
connection = connection_class(url.hostname, url.port, timeout=15)
connection.request("POST", image_list, (oversized_body[i:i + 32768] for i in range(0, len(oversized_body), 32768)),
                   headers={"Authorization": "Bearer " + alice_token,
                            "Content-Type": "multipart/form-data; boundary=oversize"}, encode_chunked=True)
response = connection.getresponse()
assert response.status == 413
assert json.loads(response.read())["code"] == "payload_too_large"
connection.close()
uploaded = [upload(png, 201) for _ in range(4)]
with ThreadPoolExecutor(max_workers=2) as workers:
    outcomes = list(workers.map(lambda _: upload(png, (201, 409)), range(2)))
assert sorted(outcomes) == [201, 409]
assert upload(png, 409)["code"] == "image_limit_reached"
first_image = image_list + "/" + uploaded[0]["id"]
assert request("GET", first_image, 200, token=alice_token, raw_response=True) == png
request("DELETE", first_image, 403, token=bob_token)
request("DELETE", first_image, 204, token=alice_token)
request("GET", first_image, 404, token=alice_token)
request('POST',temporary_path+'/publish',204,token=alice_token)
assert len(request("GET", image_list, 200)) == 4
request('GET',temporary_path,200)
request('DELETE',temporary_path,204,token=alice_token)
request('GET',temporary_path,404)
assert request('GET',listings+f'?search=Delete%20me%20{suffix}',200)['totalCount']==0
offer=request('POST',listings,200,listing,bob_token)['id']
request('POST',listings+'/'+offer+'/publish',204,token=bob_token)
exchanges = "/api/exchanges"
offer_body = {"listingId": item, "offeredListingId": offer, "requesterId": alice["id"]}
request("POST", exchanges, 403, offer_body, alice_token)
other_offer = request("POST", listings, 200, listing, bob_token)["id"]
request("POST", f"{listings}/{other_offer}/publish", 204, token=bob_token)
request("POST", exchanges, 400, {"listingId": other_offer, "offeredListingId": offer}, bob_token)
exchange = request("POST", exchanges, 200, offer_body, bob_token)["id"]
exchange_path = f"{exchanges}/{exchange}"
sent_exchanges = request("GET", exchanges + "/sent", 200, token=bob_token)
received_exchanges = request("GET", exchanges + "/received", 200, token=alice_token)
assert [x["id"] for x in sent_exchanges] == [exchange]
assert [x["id"] for x in received_exchanges] == [exchange]
assert sent_exchanges[0]["canAccept"] is False and received_exchanges[0]["canAccept"] is True
assert received_exchanges[0]["counterparty"] == {"id": bob["id"], "name": "Bob"}
assert received_exchanges[0]["listing"]["id"] == item
assert received_exchanges[0]["offeredListing"]["id"] == offer
request("POST", exchange_path + "/reject", 403, token=bob_token)
assert request("POST", exchange_path + "/reject", 200, token=alice_token)["status"] == "Rejected"
exchange = request("POST", exchanges, 200, offer_body, bob_token)["id"]
exchange_path = f"{exchanges}/{exchange}"
request("POST", exchange_path + "/cancel", 403, token=alice_token)
assert request("POST", exchange_path + "/cancel", 200, token=bob_token)["status"] == "Cancelled"
exchange = request("POST", exchanges, 200, offer_body, bob_token)["id"]
exchange_path = f"{exchanges}/{exchange}"
request("POST", exchange_path + "/accept", 403, {"actorId": alice["id"]}, bob_token)
request("POST", exchange_path + "/complete", 409, token=bob_token)
request("POST", exchange_path + "/accept", 204, token=alice_token)
request("DELETE", item_path, 409, token=alice_token)
assert request("GET", listings + f"?search={suffix}&available=false", 200)["totalCount"] == 1
assert request("GET", f"{listings}/{offer}", 200)["status"] == "InExchange"
request("POST", exchange_path + "/cancel", 409, token=bob_token)
assert request("POST", exchange_path + "/complete", 200, token=bob_token)["status"] == "Completed"
assert [x["status"] for x in request("GET", listings + "/mine", 200, token=alice_token)["items"]] == ["Completed"]
bob_listings = request("GET", listings + "/mine", 200, token=bob_token)["items"]
assert next(x for x in bob_listings if x["id"] == offer)["status"] == "Completed"
contest = request("POST", listings, 200, listing, alice_token)["id"]
request("POST", f"{listings}/{contest}/publish", 204, token=alice_token)
second_offer = request("POST", listings, 200, listing, bob_token)["id"]
request("POST", f"{listings}/{second_offer}/publish", 204, token=bob_token)
competing_ids = [
    request("POST", exchanges, 200, {"listingId": contest, "offeredListingId": offer_id}, bob_token)["id"]
    for offer_id in (other_offer, second_offer)
]
with ThreadPoolExecutor(max_workers=2) as workers:
    acceptances = list(workers.map(
        lambda id: request("POST", f"{exchanges}/{id}/accept", (204, 409), token=alice_token), competing_ids))
assert sorted(acceptances) == [204, 409]
contest_rows = [entry for entry in request("GET", exchanges + "/received", 200, token=alice_token)
                if entry["id"] in competing_ids]
assert {entry["status"] for entry in contest_rows} == {"Accepted", "Pending"}
assert not any(entry["canAccept"] for entry in contest_rows)
stale_exchange = next(entry for entry in contest_rows if entry["status"] == "Pending")
conflict = request("POST", f"{exchanges}/{stale_exchange['id']}/accept", 409, token=alice_token)
assert conflict["code"] == "listing_unavailable"

reservable = request("POST", listings, 200, listing, alice_token)["id"]
request("POST", f"{listings}/{reservable}/publish", 204, token=alice_token)
reservations = "/api/reservations"
reservation_body = {"listingId": reservable, "requesterId": alice["id"]}
request("POST", reservations, 400, reservation_body, alice_token)
pending = request("POST", reservations, 201, reservation_body, bob_token)
assert pending["requesterId"] == bob["id"] and pending["status"] == "Pending"
duplicate = request("POST", reservations, 409, reservation_body, bob_token)
assert duplicate["code"] == "reservation_already_pending"
sent_reservations = request("GET", reservations + "/sent", 200, token=bob_token)
received_reservations = request("GET", reservations + "/received", 200, token=alice_token)
assert [x["id"] for x in sent_reservations] == [pending["id"]]
assert [x["id"] for x in received_reservations] == [pending["id"]]
assert sent_reservations[0]["canAccept"] is False and received_reservations[0]["canAccept"] is True
assert sent_reservations[0]["counterparty"] == {"id": alice["id"], "name": "Alice Updated"}
assert received_reservations[0]["listing"]["title"] == listing["title"]
path = reservations + "/" + pending["id"]
request("POST", path + "/accept", 403, token=bob_token)
request("POST", path + "/cancel", 403, token=alice_token)
assert request("POST", path + "/cancel", 200, token=bob_token)["status"] == "Cancelled"
pending = request("POST", reservations, 201, reservation_body, bob_token)
path = reservations + "/" + pending["id"]
assert request("POST", path + "/reject", 200, token=alice_token)["status"] == "Rejected"
pending = request("POST", reservations, 201, reservation_body, bob_token)
path = reservations + "/" + pending["id"]
assert request("POST", path + "/accept", 200, token=alice_token)["status"] == "Accepted"
assert request("GET", f"{listings}/{reservable}", 200)["status"] == "Reserved"
request("POST", reservations, 409, reservation_body, bob_token)
request("POST", path + "/cancel", 409, token=bob_token)
moderation = "/api/moderation"
request("GET", moderation + "/users", 401)
request("GET", moderation + "/users", 403, token=alice_token)
request("GET", moderation + "/listings", 403, token=alice_token)
if admin_token:
    pending_review = request("POST", listings, 200, dict(listing, title=f"Moderate {suffix}"), alice_token)["id"]
    public_path = f"{listings}/{pending_review}"
    review_path = f"{moderation}/listings/{pending_review}"
    request("POST", public_path + "/publish", 204, token=alice_token)
    request("GET", public_path, 200)
    cover = request("POST", public_path + "/images", 201,
                    body=b"--test-cover\r\nContent-Disposition: form-data; name=\"file\"; filename=\"cover.png\"\r\n"
                         b"Content-Type: image/png\r\n\r\n" + png + b"\r\n--test-cover--\r\n",
                    token=alice_token, content_type="multipart/form-data; boundary=test-cover")
    review_request = request("POST", reservations, 201, {"listingId": pending_review}, bob_token)
    before_hide = next(x for x in request("GET", reservations + "/sent", 200, token=bob_token)
                       if x["id"] == review_request["id"])
    assert before_hide["listing"]["coverImageId"] == cover["id"]
    assert any(x["id"] == alice["id"] for x in request("GET", moderation + "/users", 200, token=admin_token))
    queue = request("GET", moderation + "/listings", 200, token=admin_token)
    assert any(x["id"] == pending_review for x in queue["items"])
    request("POST", review_path + "/hide", 403, {"reason": "Violation"}, alice_token)
    request("POST", review_path + "/hide", 400, {"reason": " "}, admin_token)
    hidden = request("POST", review_path + "/hide", 200, {"reason": "Violation"}, admin_token)
    assert hidden["status"] == "Hidden" and hidden["moderationReason"] == "Violation"
    request("GET", public_path, 404)
    assert request("GET", f"{listings}/mine/{pending_review}", 200, token=alice_token)["status"] == "Hidden"
    after_hide = next(x for x in request("GET", reservations + "/sent", 200, token=bob_token)
                      if x["id"] == review_request["id"])
    assert after_hide["listing"] is None
    after_hide_owner = next(x for x in request("GET", reservations + "/received", 200, token=alice_token)
                            if x["id"] == review_request["id"])
    assert after_hide_owner["status"] == "Pending" and after_hide_owner["canAccept"] is False
    assert request("POST", f"{reservations}/{review_request['id']}/accept", 409,
                   token=alice_token)["code"] == "listing_unavailable"
    assert request("GET", listings + f"?search=Moderate%20{suffix}", 200)["totalCount"] == 0
    assert request("POST", review_path + "/return-to-draft", 200, token=admin_token)["status"] == "Draft"
    request("POST", public_path + "/publish", 204, token=alice_token)
    rejected = request("POST", review_path + "/reject", 200, {"reason": "Violation"}, admin_token)
    assert rejected["status"] == "Rejected"
    request("GET", public_path, 404)
    assert request("GET", listings + f"?search=Moderate%20{suffix}", 200)["totalCount"] == 0
    request("POST", review_path + "/remove", 204, {"reason": "Violation"}, admin_token)
    assert not any(x["id"] == pending_review for x in request("GET", listings + "/mine", 200, token=alice_token)["items"])
read_paths = [listings, listings + "?search=textbook&available=true", f"{listings}/{reservable}"]
read_times = []
for url in read_paths:
    start = time.perf_counter()
    request("GET", url, 200)
    read_times.append(time.perf_counter() - start)
print("Local list, search and detail latency (ms):", [round(t * 1000) for t in read_times])
assert max(read_times) < 2

request('POST',auth+'/logout',204,token=alice_token)
request('GET',profile,401,token=alice_token)
request('GET',profile,200,token=another_session)
print('HTTP registration, validation, duplicates, login, admin bootstrap, profile, listing CRUD/search, image validation and permissions, ownership, reservations, exchange, moderation and logout passed.')
