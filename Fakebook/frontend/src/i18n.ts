export type Locale = 'en' | 'vi'

export const LOCALE_STORAGE_KEY = 'fb.locale'

const en = {
  appLoading: 'Loading Fakebook',
  languageLabel: 'Language',
  searchPlaceholder: 'Search Fakebook',
  primaryNavLabel: 'Primary',
  home: 'Home',
  watch: 'Watch',
  groups: 'Groups',
  marketplace: 'Marketplace',
  notifications: 'Notifications',
  demoData: 'Demo data',
  usingLocalDemoData: 'Using local demo data',
  navigationLabel: 'Navigation',
  newsFeed: 'News Feed',
  friends: 'Friends',
  saved: 'Saved',
  yourShortcuts: 'Your shortcuts',
  createPost: 'Create post',
  composerPlaceholder: "What's on your mind, {name}?",
  live: 'Live',
  photo: 'Photo',
  feeling: 'Feeling',
  postPrivacy: 'Post privacy',
  privacyFriends: 'Friends',
  privacyPublic: 'Public',
  privacyOnlyMe: 'Only me',
  post: 'Post',
  edited: 'Edited',
  editPost: 'Edit post',
  deletePost: 'Delete post',
  morePostOptions: 'More post options',
  cancel: 'Cancel',
  save: 'Save',
  beFirstToReact: 'Be the first to react',
  reactionLike: 'Like',
  reactionLove: 'Love',
  reactionHaha: 'Haha',
  reactionWow: 'Wow',
  reactionSad: 'Sad',
  reactionAngry: 'Angry',
  commentCountLabel: 'comments',
  shareCountLabel: 'shares',
  commentAction: 'Comment',
  shareAction: 'Share',
  writeComment: 'Write a comment...',
  send: 'Send',
  profileContactsLabel: 'Profile and contacts',
  editProfile: 'Edit profile',
  posts: 'Posts',
  friendRequests: 'Friend requests',
  mutualFriendsLabel: 'mutual friends',
  confirm: 'Confirm',
  delete: 'Delete',
  contacts: 'Contacts',
  online: 'Online',
  recentActivity: 'Recent activity',
  closeProfileEditor: 'Close profile editor',
  nameLabel: 'Name',
  bioLabel: 'Bio',
  locationLabel: 'Location',
  avatarUrlLabel: 'Avatar URL',
  saveProfile: 'Save profile',
  justNow: 'Just now',
  minuteShort: '{count}m',
  hourShort: '{count}h',
  dayShort: '{count}d',
}

export type Messages = typeof en

const vi: Record<keyof Messages, string> = {
  appLoading: 'Đang tải Fakebook',
  languageLabel: 'Ngôn ngữ',
  searchPlaceholder: 'Tìm kiếm trên Fakebook',
  primaryNavLabel: 'Điều hướng chính',
  home: 'Trang chủ',
  watch: 'Video',
  groups: 'Nhóm',
  marketplace: 'Chợ',
  notifications: 'Thông báo',
  demoData: 'Dữ liệu mẫu',
  usingLocalDemoData: 'Đang dùng dữ liệu mẫu trên máy',
  navigationLabel: 'Điều hướng',
  newsFeed: 'Bảng tin',
  friends: 'Bạn bè',
  saved: 'Đã lưu',
  yourShortcuts: 'Lối tắt của bạn',
  createPost: 'Tạo bài viết',
  composerPlaceholder: 'Bạn đang nghĩ gì, {name}?',
  live: 'Trực tiếp',
  photo: 'Ảnh',
  feeling: 'Cảm xúc',
  postPrivacy: 'Quyền riêng tư bài viết',
  privacyFriends: 'Bạn bè',
  privacyPublic: 'Công khai',
  privacyOnlyMe: 'Chỉ mình tôi',
  post: 'Đăng',
  edited: 'Đã chỉnh sửa',
  editPost: 'Chỉnh sửa bài viết',
  deletePost: 'Xóa bài viết',
  morePostOptions: 'Thêm tùy chọn bài viết',
  cancel: 'Hủy',
  save: 'Lưu',
  beFirstToReact: 'Hãy là người đầu tiên bày tỏ cảm xúc',
  reactionLike: 'Thích',
  reactionLove: 'Yêu thích',
  reactionHaha: 'Haha',
  reactionWow: 'Wow',
  reactionSad: 'Buồn',
  reactionAngry: 'Phẫn nộ',
  commentCountLabel: 'bình luận',
  shareCountLabel: 'lượt chia sẻ',
  commentAction: 'Bình luận',
  shareAction: 'Chia sẻ',
  writeComment: 'Viết bình luận...',
  send: 'Gửi',
  profileContactsLabel: 'Hồ sơ và liên hệ',
  editProfile: 'Chỉnh sửa hồ sơ',
  posts: 'Bài viết',
  friendRequests: 'Lời mời kết bạn',
  mutualFriendsLabel: 'bạn chung',
  confirm: 'Xác nhận',
  delete: 'Xóa',
  contacts: 'Liên hệ',
  online: 'Đang hoạt động',
  recentActivity: 'Hoạt động gần đây',
  closeProfileEditor: 'Đóng trình chỉnh sửa hồ sơ',
  nameLabel: 'Tên',
  bioLabel: 'Tiểu sử',
  locationLabel: 'Vị trí',
  avatarUrlLabel: 'URL ảnh đại diện',
  saveProfile: 'Lưu hồ sơ',
  justNow: 'Vừa xong',
  minuteShort: '{count} phút',
  hourShort: '{count} giờ',
  dayShort: '{count} ngày',
}

export const messages: Record<Locale, Messages> = { en, vi }

export const languageOptions: { locale: Locale; label: string; shortLabel: string }[] = [
  { locale: 'en', label: 'English', shortLabel: 'EN' },
  { locale: 'vi', label: 'Tiếng Việt', shortLabel: 'VI' },
]

export function isLocale(value: string | null): value is Locale {
  return value === 'en' || value === 'vi'
}

export function getInitialLocale(): Locale {
  if (typeof window === 'undefined') {
    return 'en'
  }

  try {
    const stored = window.localStorage.getItem(LOCALE_STORAGE_KEY)
    if (isLocale(stored)) {
      return stored
    }
  } catch {
    /* localStorage may be unavailable in private contexts */
  }

  const browserLocales = window.navigator.languages.length ? window.navigator.languages : [window.navigator.language]
  return browserLocales.some((value) => value.toLowerCase().startsWith('vi')) ? 'vi' : 'en'
}

export function formatMessage(template: string, values: Record<string, string | number>) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => String(values[key] ?? `{${key}}`))
}
