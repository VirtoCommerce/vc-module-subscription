// Same-origin fetch for endpoints of other modules (orders, stores);
// cookie auth works because the host is same-origin.
async function http<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    credentials: "same-origin",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${method} ${url}: ${res.status}`);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const post = <T>(url: string, body: unknown) => http<T>("POST", url, body);

export interface SearchResult<T> {
  totalCount?: number;
  results?: T[];
}

// VcStatus variant of a subscription status
export function statusVariant(status?: string): "success" | "warning" | "danger" | "info" {
  switch (status) {
    case "Active":
      return "success";
    case "Trialing":
      return "info";
    case "PastDue":
    case "Unpaid":
      return "warning";
    case "Cancelled":
      return "danger";
    default:
      return "info";
  }
}
