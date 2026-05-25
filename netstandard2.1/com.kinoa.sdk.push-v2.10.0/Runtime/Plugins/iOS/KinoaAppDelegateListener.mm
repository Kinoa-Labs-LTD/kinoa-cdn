#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "AppDelegateListener.h"
#import "UnityNotificationManager.h"

@interface KinoaAppDelegateListener : NSObject <AppDelegateListener>
+ (instancetype)sharedInstance;
@end

@implementation KinoaAppDelegateListener
static KinoaAppDelegateListener* _kinoaAppDelegateListener = [[KinoaAppDelegateListener alloc] init];

+ (instancetype)sharedInstance
{
    return _kinoaAppDelegateListener;
}

- (instancetype)init
{
    self = [super init];
    if (self) {
        UnityRegisterAppDelegateListener(self);
    }
    return self;
}

- (void)dealloc
{
    [[NSNotificationCenter defaultCenter] removeObserver:self];
}

#pragma mark AppDelegateListener
- (void)didFinishLaunching:(NSNotification*)notification;
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = [UnityNotificationManager sharedInstance];
}
@end