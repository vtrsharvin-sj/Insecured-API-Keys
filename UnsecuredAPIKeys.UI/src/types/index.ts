import { SVGProps } from "react";

export type IconSvgProps = SVGProps<SVGSVGElement> & {
  size?: number;
};

export interface ApiKeyReference {
  repoURL: string;
  repoOwner: string;
  fileURL: string;
  filePath: string;
  codeContext?: string;
  lineNumber: number;
  // Additional fields that might come from API
  repoName?: string;
  fileName?: string;
  branch?: string;
  fileSHA?: string;
  provider?: string;
}

export interface ApiKey {
  id: number;
  apiType: string;
  apiKey: string;
  timesDisplayed: number;
  status: number; // ApiStatusEnum integer: -99=Unverified, 1=Valid, 0=Invalid, 3=Removed, 4=Flagged, 5=Fixed, 6=Error, 7=ValidNoCredits
  lastValidUTC: string;
  lastCheckedUTC?: string;
  firstFoundUTC: string;
  lastFoundUTC?: string;
  searchProvider?: number;
  references: ApiKeyReference[];
}

export interface KeyStats {
  totalNumberOfKeys: number;
  numberOfValidKeys: number;
  newKeysFoundToday: number;
  totalFixedKeys: number;
  mostRecentFind: string;
}

export interface ApiKeyType {
  apiType: string;
  keyCount: number;
  apiTypeId: number;
}

// Rate limiting types
export interface RateLimitInfo {
  limit: number;
  requestsRemaining: number;
  requestsCount: number;
  timeWindow: string;
  resetAt: string;
}

export interface RateLimitResponse {
  success: boolean;
  message: string;
  statusCode: number;
  rateLimit: RateLimitInfo;
  fallbackApiKey?: ApiKey;
}

export interface ApiResponse<T> {
  data?: T;
  rateLimit?: RateLimitInfo;
  error?: string;
  isRateLimited?: boolean;
  fallbackApiKey?: ApiKey;
  cancelled?: boolean;
}

export interface GitHubUserInfo {
  username: string;
  avatarUrl: string;
  userId?: number;
  displayName?: string;
  issueCount: number;
}

export interface SnitchLeaderboardEntry {
  rank: number;
  displayName: string;
  totalIssuesSubmitted: number;
  openIssuesSubmitted: number;
  closedIssuesSubmitted: number;
  totalRepositoriesAffected: number;
  favoriteApiType?: string;
  snitchScore: number;
  firstSubmissionAt: string;
  lastSubmissionAt: string;
  consecutiveDaysActive: number;
  gitHubUsers?: GitHubUserInfo[]; // Make optional since API might not return this
}

export interface LeaderboardStats {
  totalSnitches: number;
  totalIssuesSubmitted: number;
  totalIssuesVerified: number;
  openIssues: number;
  closedIssues: number;
  successRate: number;
}

export interface LeaderboardEntry {
  githubUsername: string;
  totalSubmissions: number;
  lastSubmissionDate: string;
}

// Google Analytics Types - declarations moved to avoid conflicts

export interface GtagConfig {
  page_title?: string;
  page_location?: string;
  custom_map?: { [key: string]: string };
}

export interface GtagEvent {
  action: string;
  category?: string;
  label?: string;
  value?: number;
}
