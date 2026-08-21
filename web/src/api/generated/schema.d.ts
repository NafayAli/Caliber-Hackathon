/**
 * OpenAPI schema types generated from the running Caliber API.
 *
 * Regenerate after API changes:
 *   npm run generate:api
 *
 * Requires the API to be running at https://localhost:7143 (or set VITE_API_PROXY_TARGET).
 */
export interface paths {
  '/api/dashboard': {
    get: {
      responses: {
        200: {
          content: {
            'application/json': Record<string, unknown>
          }
        }
      }
    }
  }
}

export type ApiPaths = keyof paths
