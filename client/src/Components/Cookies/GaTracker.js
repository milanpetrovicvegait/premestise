import {useEffect} from 'react';
import Cookies from 'js-cookie';
import ReactGA from 'react-ga';

const useGaTracker = () => {
    useEffect(() => {
        if(Cookies.get('CookieConsent')){
          ReactGA.initialize('UA-217414427-1');
          ReactGA.pageview(window.location.pathname + window.location.search);
        }
      });
};

export default useGaTracker;