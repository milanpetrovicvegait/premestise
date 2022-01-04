//http://localhost:50801
export const WEB_API_URL = "";
export const MAP_URL = "https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=";
export const MAP_ACCESS_TOKEN = "pk.eyJ1IjoidGVzdGFwcGNmYWMiLCJhIjoiY2sxZXRjazlsMGw0ZzNvdW1mdTh4ZHA0eCJ9.7RLE9_R1Z-SOpPw9WrRJCA";
export const PUBLIC_DOMAIN_URL = "http://premestise.sitesstage.com";
export function getBaseUrl() {
    const loc = window.location;
    return loc.protocol + "//" + loc.host;
}