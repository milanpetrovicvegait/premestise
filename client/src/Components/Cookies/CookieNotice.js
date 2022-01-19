import React, { Component } from "react";
import CookieConsent from "react-cookie-consent";
import ReactGA from 'react-ga';

class CookieNotice extends Component {
  render() {
    return (
        <CookieConsent
            cookieName="CookieConsent"
            location="bottom"
            enableDeclineButton={true}
            buttonText="Prihvatam"
            declineButtonText="Ne prihvatam"
            declineButtonStyle={{ background: "#67d5f480", color: "#ffffff", fontSize: "13px", textShadow: "0 0 1px #000000" }}
            buttonStyle={{ background: "#67d5f4", color: "#ffffff", fontSize: "13px", textShadow: "0 0 1px #000000" }}
            style={{ background: "#346b79", borderTop: "2px solid #67d5f4", boxShadow: "-1px 0 16px rgba(0, 0, 0, 0.18)"}}
            onAccept={() => {ReactGA.initialize('UA-217414427-1');
              ReactGA.pageview(window.location.pathname + window.location.search);}}
            expires={365}
        >
            Ovaj sajt koristi kolacice
        </CookieConsent>
    );
  }
}

export default CookieNotice;
